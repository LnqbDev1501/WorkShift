import 'package:flutter/material.dart';
import '../../models/order_model.dart';
import '../../models/product_model.dart';
import '../../services/staff_order_service.dart';
import '../../services/product_service.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_text_field.dart';
import '../../utils/format_utils.dart';

class CreateOrderView extends StatefulWidget {
  const CreateOrderView({super.key});
  @override
  State<CreateOrderView> createState() => _CreateOrderViewState();
}

class _CreateOrderViewState extends State<CreateOrderView> {
  final _formKey = GlobalKey<FormState>();
  final _noteController = TextEditingController();
  final _orderService = StaffOrderService();
  final _productService = ProductService();
  bool _isLoading = false;
  String? _errorMessage;
  final List<_ItemBuilder> _items = [];

  @override
  void initState() { super.initState(); _addItem(); }

  @override
  void dispose() {
    _noteController.dispose();
    for (final i in _items) { i.dispose(); }
    super.dispose();
  }

  void _addItem() => setState(() => _items.add(_ItemBuilder()));
  void _removeItem(int idx) { if (_items.length > 1) setState(() => _items.removeAt(idx)); }

  Future<void> _showProductPicker(int itemIndex) async {
    final result = await showModalBottomSheet<ProductModel>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      builder: (_) => _ProductPickerSheet(productService: _productService),
    );
    if (result != null) {
      setState(() {
        _items[itemIndex].product = result;
        _items[itemIndex].discount = 0;
      });
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    for (int i = 0; i < _items.length; i++) { if (!_items[i].validate()) { setState(() {}); return; } }
    setState(() => _isLoading = true);
    try {
      final req = CreateOrderRequest(
        items: _items.map((i) => CreateOrderItemRequest(productId: i.productId!, quantity: i.quantity, discountAmount: i.discount)).toList(),
        note: _noteController.text.trim().isEmpty ? null : _noteController.text.trim(),
      );
      await _orderService.createOrder(req);
      if (mounted) { ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Order created successfully'))); Navigator.pop(context, true); }
    } catch (e) { setState(() { _errorMessage = e.toString(); _isLoading = false; }); }
  }

  double get _total => _items.fold(0.0, (s, i) => s + i.lineTotal);
  double get _discount => _items.fold(0.0, (s, i) => s + i.discount * i.quantity);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Create Order', style: TextStyle(fontWeight: FontWeight.w700)),
        backgroundColor: AppColors.surface, elevation: 0, centerTitle: true,
        leading: IconButton(icon: const Icon(Icons.arrow_back_rounded), onPressed: () => Navigator.pop(context)),
      ),
      body: Form(
        key: _formKey,
        child: Column(children: [
          if (_errorMessage != null)
            Container(
              margin: const EdgeInsets.all(16), padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(color: AppColors.errorBg, borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppColors.error.withValues(alpha: 0.3))),
              child: Row(children: [
                const Icon(Icons.error_outline_rounded, color: AppColors.error, size: 20),
                const SizedBox(width: 8),
                Expanded(child: Text(_errorMessage!, style: const TextStyle(color: AppColors.error))),
                IconButton(icon: const Icon(Icons.close_rounded, color: AppColors.error, size: 20), onPressed: () => setState(() => _errorMessage = null)),
              ]),
            ),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                const Text('Order Items', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                const SizedBox(height: 12),
                ..._items.asMap().entries.map((e) {
                  final idx = e.key; final item = e.value;
                  return Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Row(children: [
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                            decoration: BoxDecoration(color: AppColors.primaryLight, borderRadius: BorderRadius.circular(20)),
                            child: Text('Item ${idx + 1}', style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.primary)),
                          ),
                          const Spacer(),
                          if (idx > 0) IconButton(icon: const Icon(Icons.delete_outline_rounded, color: AppColors.error), onPressed: () => _removeItem(idx)),
                        ]),
                        const SizedBox(height: 16),
                        InkWell(
                          onTap: () => _showProductPicker(idx),
                          child: Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(color: AppColors.background, borderRadius: BorderRadius.circular(12), border: Border.all(color: AppColors.border)),
                            child: Row(children: [
                              if (item.product == null) ...[
                                const Icon(Icons.shopping_bag_outlined, color: AppColors.primary),
                                const SizedBox(width: 12),
                                const Expanded(child: Text('Select a product from catalog', style: TextStyle(fontWeight: FontWeight.w500))),
                                        const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
                                      ] else ...[
                                        const Icon(Icons.check_circle_outline, color: AppColors.success),
                                        const SizedBox(width: 12),
                                        Expanded(child: Text(item.product!.productName, style: const TextStyle(fontWeight: FontWeight.w500))),
                                        Text(formatCurrency(item.product!.price), style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.primary)),
                                      ],
                                    ]),
                                  ),
                                ),
                        const SizedBox(height: 16),
                        Row(children: [
                          Expanded(child: _buildField('Quantity', item.quantity, (v) { item.quantity = v; setState(() {}); }, item.quantityError, 1)),
                          const SizedBox(width: 12),
                          Expanded(child: _buildField('Discount', item.discount.toInt(), (v) { item.discount = v.toDouble(); setState(() {}); }, item.discountError, 0)),
                        ]),
                        const SizedBox(height: 12),
                        AppTextField(label: 'Note (Optional)', hintText: 'Special requests', controller: item.noteController, prefixIcon: Icons.note_alt_outlined, onChanged: (_) => setState(() {})),
                        const SizedBox(height: 8),
                        Align(
                          alignment: Alignment.centerRight,
                          child: Text('Line Total: ${formatCurrency(item.lineTotal)}', style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.primary)),
                        ),
                      ]),
                    ),
                  );
                }),
                if (_items.length < 10)
                  Center(child: TextButton.icon(onPressed: _addItem, icon: const Icon(Icons.add_rounded, color: AppColors.primary),
                    label: const Text('Add Item', style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w600)))),
                const SizedBox(height: 24),
                AppTextField(label: 'Order Note (Optional)', hintText: 'Special instructions, delivery info', controller: _noteController, prefixIcon: Icons.note_alt_outlined),
                const SizedBox(height: 24),
                Card(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)), child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    const Text('Order Summary', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                    const SizedBox(height: 12),
                    buildTotalRow('Subtotal', formatCurrency(_total)),
                    if (_discount > 0) buildTotalRow('Discount', '- ${formatCurrency(_discount)}', color: AppColors.success),
                    const Divider(height: 16),
                    buildTotalRow('Total', formatCurrency(_total - _discount), isTotal: true),
                  ]),
                )),
                const SizedBox(height: 24),
                AppButton(text: 'Create Order', icon: Icons.receipt_long_rounded, isLoading: _isLoading, onPressed: _submit),
                const SizedBox(height: 16),
              ],
            ),
          ),
        ]),
      ),
    );
  }

  Widget _buildField(String label, int value, void Function(int) onChanged, String? error, int min) {
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
      const SizedBox(height: 6),
      TextFormField(
        initialValue: value.toString(),
        keyboardType: TextInputType.number,
        decoration: InputDecoration(errorText: error, border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
          contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14), filled: true, fillColor: AppColors.background),
        onChanged: (val) { final p = int.tryParse(val) ?? 0; if (p >= min) onChanged(p); },
        validator: (val) { final p = int.tryParse(val ?? ''); if (p == null || p < min) return 'Invalid'; return null; },
      ),
    ]);
  }
}

class _ItemBuilder {
  ProductModel? product;
  int quantity = 1;
  double discount = 0;
  final noteController = TextEditingController();
  String? quantityError;
  String? discountError;

  int? get productId => product?.productId;

  bool validate() {
    bool ok = true;
    if (productId == null) { ok = false; }
    if (quantity < 1) { quantityError = 'Must be ≥ 1'; ok = false; } else { quantityError = null; }
    if (discount < 0) { discountError = 'Cannot be negative'; ok = false; } else { discountError = null; }
    return ok;
  }

  double get unitPrice => product?.price ?? 0;
  double get lineTotal => unitPrice * quantity - discount;
  void dispose() => noteController.dispose();
}

class _ProductPickerSheet extends StatefulWidget {
  final ProductService productService;
  const _ProductPickerSheet({required this.productService});
  @override
  State<_ProductPickerSheet> createState() => _ProductPickerSheetState();
}

class _ProductPickerSheetState extends State<_ProductPickerSheet> {
  final _searchController = TextEditingController();
  List<ProductModel> _products = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadProducts();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadProducts() async {
    setState(() { _isLoading = true; _error = null; });
    try {
      final response = await widget.productService.getProducts();
      if (mounted) setState(() { _products = response.items; _isLoading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = e.toString(); _isLoading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.7,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (_, scrollController) => Column(children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: Row(children: [
            const Expanded(child: Text('Select Product', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700))),
            IconButton(icon: const Icon(Icons.close_rounded), onPressed: () => Navigator.pop(context)),
          ]),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: TextField(
            controller: _searchController,
            decoration: InputDecoration(hintText: 'Search products...', prefixIcon: const Icon(Icons.search_rounded),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(12))),
            onChanged: (_) => setState(() {}),
          ),
        ),
        const SizedBox(height: 8),
        Expanded(
          child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
              ? Center(child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
                  const Icon(Icons.error_outline_rounded, color: AppColors.error, size: 48),
                  const SizedBox(height: 8),
                  Text(_error!, style: const TextStyle(color: AppColors.error)),
                  TextButton(onPressed: _loadProducts, child: const Text('Retry')),
                ]))
              : ListView.builder(
                  controller: scrollController,
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: _products.length,
                  itemBuilder: (_, index) {
                    final product = _products[index];
                    return Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: ListTile(
                        leading: Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(color: AppColors.primaryLight, borderRadius: BorderRadius.circular(10)),
                          child: const Icon(Icons.shopping_bag_outlined, color: AppColors.primary),
                        ),
                        title: Text(product.productName, style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text(product.categoryName, style: TextStyle(color: AppColors.textSecondary)),
                        trailing: Text(formatCurrency(product.price), style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.primary)),
                        onTap: () => Navigator.pop(context, product),
                      ),
                    );
                  },
                ),
        ),
      ]),
    );
  }
}