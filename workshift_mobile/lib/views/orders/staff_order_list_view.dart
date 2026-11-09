import 'package:flutter/material.dart';
import '../../controllers/orders/staff_order_controller.dart';
import '../../services/orders/staff_order_service.dart';
import '../../models/orders/order_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../utils/format_utils.dart';
import 'staff_order_detail_view.dart';
import 'create_order_view.dart';

class StaffOrderListView extends StatefulWidget {
  const StaffOrderListView({super.key});

  @override
  State<StaffOrderListView> createState() => _StaffOrderListViewState();
}

class _StaffOrderListViewState extends State<StaffOrderListView> {
  final _controller = StaffOrderController();
  final _scrollController = ScrollController();

  DateTime? _selectedDate = DateTime.now();
  String? _selectedStatus; // null = all
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
    _scrollController.addListener(_onScroll);
    _fetchOrders(refresh: true);
  }

  @override
  void dispose() {
    _controller.removeListener(_onUpdate);
    _scrollController.dispose();
    super.dispose();
  }

  void _onUpdate() {
    if (mounted) setState(() {});
  }

  void _onScroll() {
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 200) {
      if (_controller.hasMore && !_controller.isLoading) {
        _controller.loadOrders();
      }
    }
  }

  void _fetchOrders({bool refresh = true}) {
    DateTime? dateFrom;
    DateTime? dateTo;
    if (_selectedDate != null) {
      dateFrom = DateTime(
        _selectedDate!.year,
        _selectedDate!.month,
        _selectedDate!.day,
      );
      dateTo = DateTime(
        _selectedDate!.year,
        _selectedDate!.month,
        _selectedDate!.day,
      );
    }

    _controller.loadOrders(
      query: OrderListQuery(
        dateFrom: dateFrom,
        dateTo: dateTo,
        status: _selectedStatus,
        sortBy: 'oldest_pending_first',
        pageNumber: 1,
      ),
      refresh: refresh,
    );
  }

  Future<void> _onRefresh() async {
    _checkShiftStatus();
    _fetchOrders(refresh: true);
  }

  bool _isSameDay(DateTime a, DateTime b) {
    return a.year == b.year && a.month == b.month && a.day == b.day;
  }

  bool _isToday(DateTime date) {
    return _isSameDay(date, DateTime.now());
  }

  bool _isYesterday(DateTime date) {
    final yesterday = DateTime.now().subtract(const Duration(days: 1));
    return _isSameDay(date, yesterday);
  }

  String _formatDateDisplay(DateTime date) {
    final day = date.day.toString().padLeft(2, '0');
    final month = date.month.toString().padLeft(2, '0');
    final year = date.year;
    if (_isToday(date)) return 'Today ($day/$month/$year)';
    if (_isYesterday(date)) return 'Yesterday ($day/$month/$year)';
    return '$day/$month/$year';
  }

  void _changeDate(int days) {
    setState(() {
      _selectedDate = (_selectedDate ?? DateTime.now()).add(
        Duration(days: days),
      );
    });
    _fetchOrders(refresh: true);
  }

  void _resetToToday() {
    setState(() {
      _selectedDate = DateTime.now();
    });
    _fetchOrders(refresh: true);
  }

  Future<void> _pickDate() async {
    final initial = _selectedDate ?? DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(2020),
      lastDate: DateTime(2035),
    );
    if (picked != null) {
      setState(() {
        _selectedDate = picked;
      });
      _fetchOrders(refresh: true);
    }
  }

  Widget _buildDateFilterBar() {
    final isCurrentDay = _selectedDate != null && _isToday(_selectedDate!);

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 8, 16, 8),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left_rounded),
            color: _selectedDate != null
                ? AppColors.textPrimary
                : AppColors.textMuted,
            onPressed: _selectedDate != null ? () => _changeDate(-1) : null,
            tooltip: 'Previous day',
            visualDensity: VisualDensity.compact,
          ),
          Expanded(
            child: InkWell(
              onTap: _pickDate,
              borderRadius: BorderRadius.circular(10),
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 4),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(
                      isCurrentDay
                          ? Icons.today_rounded
                          : Icons.calendar_month_rounded,
                      size: 18,
                      color: isCurrentDay
                          ? AppColors.primary
                          : AppColors.textSecondary,
                    ),
                    const SizedBox(width: 8),
                    Flexible(
                      child: Text(
                        _selectedDate == null
                            ? 'All Dates'
                            : _formatDateDisplay(_selectedDate!),
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                          color: isCurrentDay
                              ? AppColors.primary
                              : AppColors.textPrimary,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    const SizedBox(width: 4),
                    const Icon(
                      Icons.arrow_drop_down_rounded,
                      size: 20,
                      color: AppColors.textMuted,
                    ),
                  ],
                ),
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right_rounded),
            color: _selectedDate != null
                ? AppColors.textPrimary
                : AppColors.textMuted,
            onPressed: _selectedDate != null ? () => _changeDate(1) : null,
            tooltip: 'Next day',
            visualDensity: VisualDensity.compact,
          ),
          if (!isCurrentDay)
            InkWell(
              onTap: _resetToToday,
              borderRadius: BorderRadius.circular(8),
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Text(
                  'Today',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                    color: AppColors.primary,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildStatusFilterChips() {
    final statuses = [
      {'key': null, 'label': 'All'},
      {'key': 'PENDING', 'label': 'Pending'},
      {'key': 'COMPLETED', 'label': 'Completed'},
      {'key': 'CANCELLED', 'label': 'Cancelled'},
    ];

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: Row(
        children: statuses.map((item) {
          final isSelected = _selectedStatus == item['key'];
          final isPending = item['key'] == 'PENDING';
          return Padding(
            padding: const EdgeInsets.only(right: 8, bottom: 8),
            child: ChoiceChip(
              label: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (isPending) ...[
                    Icon(
                      Icons.hourglass_top_rounded,
                      size: 14,
                      color: isSelected ? Colors.white : AppColors.warning,
                    ),
                    const SizedBox(width: 4),
                  ],
                  Text(item['label'] as String),
                ],
              ),
              selected: isSelected,
              onSelected: (selected) {
                if (selected) {
                  setState(() {
                    _selectedStatus = item['key'] as String?;
                  });
                  _fetchOrders(refresh: true);
                }
              },
              selectedColor: isPending ? AppColors.warning : AppColors.primary,
              labelStyle: TextStyle(
                color: isSelected ? Colors.white : AppColors.textSecondary,
                fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                fontSize: 13,
              ),
              backgroundColor: AppColors.surface,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(20),
                side: BorderSide(
                  color: isSelected ? Colors.transparent : AppColors.border,
                ),
              ),
              showCheckmark: false,
              materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            ),
          );
        }).toList(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Customer Orders',
          style: TextStyle(fontWeight: FontWeight.w700),
        ),
        backgroundColor: AppColors.surface,
        elevation: 0,
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: () => _fetchOrders(refresh: true),
            tooltip: 'Refresh',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _onRefresh,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: AppButton(
                text: 'Create New Order',
                icon: Icons.add_rounded,
                backgroundColor: _isInShift ? null : Colors.grey.shade400,
                onPressed: () async {
                  if (!_isInShift) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text(
                          'Employees who are off-duty cannot perform this action.',
                        ),
                        backgroundColor: AppColors.error,
                      ),
                    );
                    return;
                  }
                  await Navigator.push(
                    context,
                    MaterialPageRoute(builder: (_) => const CreateOrderView()),
                  );
                  _fetchOrders(refresh: true);
                },
              ),
            ),
            _buildDateFilterBar(),
            _buildStatusFilterChips(),

            Expanded(
              child: _controller.isLoading && _controller.orders.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : _controller.orders.isEmpty
                  ? Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.receipt_long_rounded,
                            size: 80,
                            color: AppColors.textMuted.withValues(alpha: 0.5),
                          ),
                          const SizedBox(height: 16),
                          Text(
                            _selectedDate != null
                                ? 'No orders for ${_formatDateDisplay(_selectedDate!)}'
                                : 'No orders yet',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: 8),
                          if (_selectedDate != null && !_isToday(_selectedDate!))
                            TextButton.icon(
                              onPressed: _resetToToday,
                              icon: const Icon(Icons.today_rounded, size: 16),
                              label: const Text('View today\'s orders'),
                            ),
                        ],
                      ),
                    )
                  : ListView.builder(
                      controller: _scrollController,
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      itemCount:
                          _controller.orders.length +
                          (_controller.hasMore ? 1 : 0),
                      itemBuilder: (_, index) {
                        if (index == _controller.orders.length) {
                          return const Padding(
                            padding: EdgeInsets.all(16),
                            child: Center(child: CircularProgressIndicator()),
                          );
                        }
                        final order = _controller.orders[index];
                        final isPending =
                            order.status.toUpperCase() == 'PENDING';

                        return Card(
                          margin: const EdgeInsets.only(bottom: 12),
                          elevation: isPending ? 2 : 0,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                            side: BorderSide(
                              color: isPending
                                  ? AppColors.warning.withValues(alpha: 0.6)
                                  : AppColors.border,
                              width: isPending ? 1.5 : 1,
                            ),
                          ),
                          child: GestureDetector(
                            onTap: () async {
                              await Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (_) => StaffOrderDetailView(
                                    orderId: order.orderId,
                                  ),
                                ),
                              );
                              _fetchOrders(refresh: true);
                            },
                            child: Padding(
                              padding: const EdgeInsets.all(16),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      if (isPending) ...[
                                        Container(
                                          margin:
                                              const EdgeInsets.only(right: 6),
                                          padding: const EdgeInsets.symmetric(
                                            horizontal: 6,
                                            vertical: 2,
                                          ),
                                          decoration: BoxDecoration(
                                            color: AppColors.warning
                                                .withValues(alpha: 0.15),
                                            borderRadius:
                                                BorderRadius.circular(6),
                                          ),
                                          child: const Row(
                                            mainAxisSize: MainAxisSize.min,
                                            children: [
                                              Icon(
                                                Icons.hourglass_top_rounded,
                                                size: 12,
                                                color: AppColors.warning,
                                              ),
                                              SizedBox(width: 2),
                                              Text(
                                                'Chờ làm',
                                                style: TextStyle(
                                                  fontSize: 11,
                                                  fontWeight: FontWeight.w700,
                                                  color: AppColors.warning,
                                                ),
                                              ),
                                            ],
                                          ),
                                        ),
                                      ],
                                      Expanded(
                                        child: Text(
                                          order.orderNumber,
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w700,
                                          ),
                                        ),
                                      ),
                                      StatusChip(
                                        label: order.status,
                                        color: getStatusColor(order.status),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 8),
                                  Row(
                                    children: [
                                      StatusChip(
                                        label: order.paymentStatus,
                                        color: getPaymentStatusColor(
                                          order.paymentStatus,
                                        ),
                                        icon: Icons.payment_rounded,
                                      ),
                                      const SizedBox(width: 8),
                                      Text(
                                        formatVietnamDateTime(order.orderDate),
                                        style: TextStyle(
                                          fontSize: 12,
                                          color: AppColors.textMuted,
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 8),
                                  ...order.items
                                      .take(3)
                                      .map(
                                        (item) => Padding(
                                          padding: const EdgeInsets.only(
                                            bottom: 4,
                                          ),
                                          child: Row(
                                            children: [
                                              Expanded(
                                                child: Text(
                                                  item.productName,
                                                  maxLines: 1,
                                                  overflow:
                                                      TextOverflow.ellipsis,
                                                ),
                                              ),
                                              Text(
                                                '${item.quantity} × ${formatCurrency(item.unitPrice)}',
                                                style: TextStyle(
                                                  fontSize: 13,
                                                  color:
                                                      AppColors.textSecondary,
                                                ),
                                              ),
                                            ],
                                          ),
                                        ),
                                      ),
                                  const Divider(height: 16),
                                  Row(
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      const Text(
                                        'Total',
                                        style: TextStyle(
                                          fontWeight: FontWeight.w600,
                                        ),
                                      ),
                                      Text(
                                        formatCurrency(order.totalAmount),
                                        style: const TextStyle(
                                          fontWeight: FontWeight.w800,
                                          color: AppColors.primary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ),
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
