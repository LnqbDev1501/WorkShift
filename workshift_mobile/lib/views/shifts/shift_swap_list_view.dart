import 'package:flutter/material.dart';
import '../../controllers/shift_swap_list_controller.dart';
import '../../models/shift_swap_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_choice_filters.dart';
import '../../widgets/app_state_panel.dart';
import 'components/shift_swap_summary.dart';
import 'shift_swap_detail_view.dart';
import 'submit_shift_swap_view.dart';

class ShiftSwapListView extends StatefulWidget {
  final ShiftSwapListController? controller;
  const ShiftSwapListView({super.key, this.controller});
  @override
  State<ShiftSwapListView> createState() => _ShiftSwapListViewState();
}

class _ShiftSwapListViewState extends State<ShiftSwapListView>
    with WidgetsBindingObserver {
  late final _controller = widget.controller ?? ShiftSwapListController();
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _controller.refresh();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) _controller.refresh();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    if (widget.controller == null) _controller.dispose();
    super.dispose();
  }

  Future<void> _openDetail(int id) async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => ShiftSwapDetailView(requestId: id)),
    );
    if (mounted) await _controller.refresh();
  }

  Future<void> _create() async {
    final result = await Navigator.of(context).push<ShiftSwapModel>(
      MaterialPageRoute(builder: (_) => const SubmitShiftSwapView()),
    );
    if (!mounted || result == null) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Swap request sent. Your colleague can now respond.'),
      ),
    );
    await _openDetail(result.swapRequestId);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Shift Swap'),
      actions: [
        IconButton(
          tooltip: 'Refresh requests',
          onPressed: _controller.refresh,
          icon: const Icon(Icons.refresh_rounded),
        ),
      ],
    ),
    bottomNavigationBar: SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
        child: AppButton(
          text: 'New swap request',
          icon: Icons.add_rounded,
          onPressed: _create,
        ),
      ),
    ),
    body: SafeArea(
      child: ListenableBuilder(
        listenable: _controller,
        builder: (context, _) => Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Exchange an assigned shift with a colleague.',
                    style: TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 10),
                  AppChoiceFilters<String>(
                    options: const [
                      ('ALL', 'All'),
                      ('RECEIVED', 'Received'),
                      ('SENT', 'Sent'),
                    ],
                    selected: _controller.direction,
                    onSelected: (direction) => _controller.setFilters(
                      direction: direction,
                      status: _controller.status,
                    ),
                  ),
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          _controller.isLoading
                              ? 'Loading requests…'
                              : '${_controller.totalItems} requests',
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                      ),
                      DropdownButton<String>(
                        value: _controller.status,
                        hint: const Text('All statuses'),
                        items: const [
                          DropdownMenuItem(
                            value: null,
                            child: Text('All statuses'),
                          ),
                          DropdownMenuItem(
                            value: 'PENDING',
                            child: Text('Pending'),
                          ),
                          DropdownMenuItem(
                            value: 'ACCEPTED',
                            child: Text('Accepted'),
                          ),
                          DropdownMenuItem(
                            value: 'REJECTED',
                            child: Text('Rejected'),
                          ),
                          DropdownMenuItem(
                            value: 'CANCELLED',
                            child: Text('Cancelled'),
                          ),
                          DropdownMenuItem(
                            value: 'EXPIRED',
                            child: Text('Expired'),
                          ),
                        ],
                        onChanged: (status) => _controller.setFilters(
                          direction: _controller.direction,
                          status: status,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: _controller.refresh,
                child: ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                  children: [
                    if (_controller.isLoading)
                      const Padding(
                        padding: EdgeInsets.all(40),
                        child: Center(child: CircularProgressIndicator()),
                      )
                    else if (_controller.error != null)
                      AppStatePanel(
                        icon: Icons.cloud_off_outlined,
                        title: 'Couldn’t load requests',
                        message: _controller.error!,
                        actionLabel: 'Try again',
                        onAction: _controller.refresh,
                        isError: true,
                      )
                    else if (_controller.items.isEmpty)
                      AppStatePanel(
                        icon: Icons.swap_horiz_rounded,
                        title: 'No swap requests',
                        message:
                            'Requests matching these filters will appear here. You can also create a new request.',
                      )
                    else ...[
                      for (final request in _controller.items) _card(request),
                      if (_controller.loadMoreError != null)
                        Text(
                          _controller.loadMoreError!,
                          style: const TextStyle(color: AppColors.error),
                        ),
                      if (_controller.hasMore)
                        AppButton(
                          text: 'Load more',
                          type: AppButtonType.outline,
                          isLoading: _controller.isLoadingMore,
                          onPressed: _controller.loadMore,
                        ),
                      const SizedBox(height: 8),
                      const Text(
                        'All shift times use Vietnam time · UTC+7',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          fontSize: 11,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    ),
  );

  Widget _card(ShiftSwapModel request) {
    final sent = _controller.isSent(request);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Card(
        child: GestureDetector(
          onTap: () => _openDetail(request.swapRequestId),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        '${sent ? 'To' : 'From'} ${sent ? request.targetUserName : request.requesterName}',
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                    ),
                    const SizedBox(width: 8),
                    ShiftSwapStatusBadge(
                      status: request.statusAt(DateTime.now()),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                ShiftSwapSummary(
                  compact: true,
                  give: sent
                      ? request.sourceAssignment
                      : request.targetAssignment,
                  receive: sent
                      ? request.targetAssignment
                      : request.sourceAssignment,
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        '${sent ? 'Sent' : 'Received'} · #${request.swapRequestId}',
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ),
                    const Text(
                      'View details',
                      style: TextStyle(
                        fontSize: 12,
                        color: AppColors.primary,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const Icon(
                      Icons.chevron_right_rounded,
                      color: AppColors.primary,
                      size: 18,
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
