import 'package:flutter/material.dart';
import '../../controllers/orders/staff_order_controller.dart';
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

  @override
  void initState() {
    super.initState();
    _controller.addListener(_onUpdate);
    _scrollController.addListener(_onScroll);
    _controller.loadOrders();
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

  Future<void> _onRefresh() => _controller.loadOrders(refresh: true);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'My Orders',
          style: TextStyle(fontWeight: FontWeight.w700),
        ),
        backgroundColor: AppColors.surface,
        elevation: 0,
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: () => _controller.loadOrders(refresh: true),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _onRefresh,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: AppButton(
                text: 'Create New Order',
                icon: Icons.add_rounded,
                onPressed: () async {
                  await Navigator.push(
                    context,
                    MaterialPageRoute(builder: (_) => const CreateOrderView()),
                  );
                  _controller.loadOrders(refresh: true);
                },
              ),
            ),
            if (_controller.errorMessage != null)
              Container(
                margin: const EdgeInsets.symmetric(horizontal: 16),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.errorBg,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                    color: AppColors.error.withValues(alpha: 0.3),
                  ),
                ),
                child: Row(
                  children: [
                    const Icon(
                      Icons.error_outline_rounded,
                      color: AppColors.error,
                      size: 20,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        _controller.errorMessage!,
                        style: const TextStyle(color: AppColors.error),
                      ),
                    ),
                    IconButton(
                      icon: const Icon(
                        Icons.close_rounded,
                        color: AppColors.error,
                        size: 20,
                      ),
                      onPressed: _controller.clearError,
                    ),
                  ],
                ),
              ),
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
                          const Text(
                            'No orders yet',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w600,
                            ),
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
                        return Card(
                          margin: const EdgeInsets.only(bottom: 12),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
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
                              _controller.loadOrders(refresh: true);
                            },
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
                                        formatDate(order.orderDate),
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
