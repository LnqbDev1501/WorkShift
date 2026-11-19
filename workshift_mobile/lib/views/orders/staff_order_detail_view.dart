import 'package:flutter/material.dart';
import '../../controllers/orders/staff_order_controller.dart';
import '../../services/orders/staff_order_service.dart';
import '../../models/orders/order_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../utils/format_utils.dart';

class StaffOrderDetailView extends StatefulWidget {
  final int orderId;
  const StaffOrderDetailView({super.key, required this.orderId});
  @override
  State<StaffOrderDetailView> createState() => _StaffOrderDetailViewState();
}

class _StaffOrderDetailViewState extends State<StaffOrderDetailView> {
  final _controller = StaffOrderController();
  final StaffOrderService _orderService = StaffOrderService();
  bool _isInShift = false;

  Future<void> _checkShiftStatus() async {
    final inShift = await _orderService.getShiftStatus();
    if (mounted) setState(() => _isInShift = inShift);
  }

  @override
  void initState() {
    super.initState();
    _checkShiftStatus();
    _controller.addListener(_onUpdate);
    _controller.loadOrderDetail(widget.orderId);
  }

  @override
  void dispose() {
    _controller.removeListener(_onUpdate);
    super.dispose();
  }

  void _onUpdate() {
    if (mounted) setState(() {});
  }

  Future<void> _handleComplete() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Complete Order'),
        content: const Text('Mark as completed? This cannot be undone.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('No'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Yes'),
          ),
        ],
      ),
    );
    if (confirm == true) {
      final ok = await _controller.completeOrder(widget.orderId);
      if (ok && mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Order completed')));
        Navigator.pop(context, true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final order = _controller.selectedOrder;
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          order?.orderNumber ?? 'Order Detail',
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        backgroundColor: AppColors.surface,
        elevation: 0,
        centerTitle: true,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: _controller.isLoading && order == null
          ? const Center(child: CircularProgressIndicator())
          : order == null
          ? Center(
              child: AppButton(
                text: 'Back to List',
                type: AppButtonType.outline,
                onPressed: () => Navigator.pop(context),
              ),
            )
          : RefreshIndicator(
              onRefresh: () => _controller.loadOrderDetail(widget.orderId),
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: _buildBodyChildren(order),
                ),
              ),
            ),
    );
  }

  List<Widget> _buildBodyChildren(dynamic order) {
    final items = (order.items as List<dynamic>).cast<OrderItemModel>();
    final payments = (order.payments as List<dynamic>).cast<PaymentModel>();
    final children = <Widget>[
      Card(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      order.orderNumber,
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  StatusChip(
                    label: order.status,
                    color: getStatusColor(order.status),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  StatusChip(
                    label: order.paymentStatus,
                    color: getPaymentStatusColor(order.paymentStatus),
                    icon: Icons.payment_rounded,
                  ),
                  const SizedBox(width: 8),
                  Text(
                    formatDate(order.orderDate),
                    style: TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
              if (order.paidAt != null) ...[
                const SizedBox(height: 8),
                Text(
                  'Paid: ${formatDate(order.paidAt!)}',
                  style: const TextStyle(
                    color: AppColors.success,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
      const SizedBox(height: 16),
      Card(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Items (${items.length})',
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 12),
              ...items.asMap().entries.map((e) {
                final i = e.key;
                final item = e.value;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${i + 1}. ${item.productName}',
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          Text(
                            '${item.quantity} × ${formatCurrency(item.unitPrice)}',
                            style: TextStyle(color: AppColors.textSecondary),
                          ),
                          const Spacer(),
                          Text(
                            formatCurrency(item.totalAmount),
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              color: AppColors.primary,
                            ),
                          ),
                        ],
                      ),
                      if (item.note != null && item.note!.isNotEmpty) ...[
                        const SizedBox(height: 8),
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(
                              Icons.note_alt_outlined,
                              size: 14,
                              color: AppColors.textSecondary,
                            ),
                            const SizedBox(width: 6),
                            Expanded(
                              child: Text(
                                item.note!,
                                style: TextStyle(
                                  fontSize: 12,
                                  color: AppColors.textSecondary,
                                  fontStyle: FontStyle.italic,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ] else ...[
                        const SizedBox(height: 8),
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(
                              Icons.note_alt_outlined,
                              size: 14,
                              color: AppColors.textSecondary,
                            ),
                            const SizedBox(width: 6),
                            Text(
                              'None',
                              style: TextStyle(
                                fontSize: 12,
                                color: AppColors.textSecondary,
                                fontStyle: FontStyle.italic,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ],
                  ),
                );
              }),
              const Divider(height: 24),
              buildTotalRow('Subtotal', formatCurrency(order.subtotal)),
              const Divider(height: 16),
              buildTotalRow(
                'Total',
                formatCurrency(order.totalAmount),
                isTotal: true,
              ),
            ],
          ),
        ),
      ),
      if (payments.isNotEmpty) ...[
        const SizedBox(height: 16),
        Card(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Payments',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 12),
                ...payments.map(
                  (p) => Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Row(
                      children: [
                        Expanded(child: Text(p.paymentMethod)),
                        Text(
                          formatCurrency(p.amount),
                          style: const TextStyle(
                            fontWeight: FontWeight.w700,
                            color: AppColors.success,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
      if (order.note != null && order.note!.isNotEmpty) ...[
        const SizedBox(height: 24),
        Card(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Icon(
                      Icons.note_alt_outlined,
                      size: 20,
                      color: AppColors.primary,
                    ),
                    const SizedBox(width: 8),
                    const Text(
                      'Order Note',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Text(
                  order.note!,
                  style: const TextStyle(
                    fontSize: 14,
                    color: AppColors.textPrimary,
                  ),
                ),
              ],
            ),
          ),
        ),
      ] else ...[
        const SizedBox(height: 24),
        Card(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Icon(
                      Icons.note_alt_outlined,
                      size: 20,
                      color: AppColors.primary,
                    ),
                    const SizedBox(width: 8),
                    const Text(
                      'Order Note',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Text(
                  'None',
                  style: TextStyle(
                    fontSize: 14,
                    color: AppColors.textSecondary,
                    fontStyle: FontStyle.italic,
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
      const SizedBox(height: 24),
      ..._buildActionButtons(order),
      const SizedBox(height: 24),
    ];
    return children;
  }

  List<Widget> _buildActionButtons(dynamic order) {
    final buttons = <Widget>[];
    final isPending = order.status == 'PENDING';

    if (isPending) {
      buttons.add(
        AppButton(
          text: 'Complete',
          icon: Icons.check_circle_rounded,
          backgroundColor: _isInShift ? null : Colors.grey.shade400,
          isLoading: _controller.isLoading,
          onPressed: () async {
            final inShift = await _orderService.getShiftStatus();
            if (!mounted) return;
            setState(() => _isInShift = inShift);
            if (!inShift) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'You must be in your shift and checked in to perform this action.',
                  ),
                  backgroundColor: AppColors.error,
                ),
              );
              return;
            }
            _handleComplete();
          },
        ),
      );
    }
    return buttons;
  }
}
