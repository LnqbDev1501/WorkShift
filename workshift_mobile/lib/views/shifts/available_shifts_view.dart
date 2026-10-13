import 'package:flutter/material.dart';

import '../../controllers/available_shifts_controller.dart';
import '../../models/available_shift_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_state_panel.dart';
import 'components/available_shift_card.dart';
import 'components/available_shift_filters.dart';
import 'components/shift_display_format.dart';

class AvailableShiftsView extends StatefulWidget {
  final AvailableShiftsController? controller;
  const AvailableShiftsView({super.key, this.controller});

  @override
  State<AvailableShiftsView> createState() => _AvailableShiftsViewState();
}

class _AvailableShiftsViewState extends State<AvailableShiftsView>
    with WidgetsBindingObserver {
  late final AvailableShiftsController _controller;

  @override
  void initState() {
    super.initState();
    _controller = widget.controller ?? AvailableShiftsController();
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

  Future<void> _chooseDates() async {
    final today = _controller.today;
    final lastDate = DateTime(today.year + 2, today.month, today.day);
    final start = _controller.fromDate;
    final end = _controller.toDate;
    final range = await showDateRangePicker(
      context: context,
      firstDate: today,
      lastDate: lastDate,
      currentDate: today,
      initialDateRange:
          start != null &&
              end != null &&
              !start.isBefore(today) &&
              !end.isAfter(lastDate)
          ? DateTimeRange(start: start, end: end)
          : null,
      helpText: 'Choose shift dates',
      saveText: 'Apply',
    );
    if (!mounted || range == null) return;
    await _controller.selectDateRange(range.start, range.end);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: const BackButton(),
        title: const Text('Available Shifts'),
        actions: [
          ListenableBuilder(
            listenable: _controller,
            builder: (context, _) => IconButton(
              tooltip: 'Refresh shifts',
              icon: const Icon(Icons.refresh_rounded),
              onPressed: _controller.isLoading ? null : _controller.refresh,
            ),
          ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: ListenableBuilder(
          listenable: _controller,
          builder: (context, _) {
            final shifts = _controller.shifts;
            return RefreshIndicator(
              onRefresh: _controller.refresh,
              child: CustomScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  SliverToBoxAdapter(child: _buildFilters()),
                  if (_controller.isLoading && shifts.isNotEmpty)
                    const SliverToBoxAdapter(
                      child: LinearProgressIndicator(minHeight: 2),
                    ),
                  if (_controller.errorMessage != null && shifts.isNotEmpty)
                    SliverToBoxAdapter(
                      child: MaterialBanner(
                        backgroundColor: AppColors.warningBg,
                        content: Text(
                          'Couldn’t refresh. Showing previous results.\n${_controller.errorMessage!}',
                          style: const TextStyle(fontSize: 13),
                        ),
                        actions: [
                          TextButton(
                            onPressed: _controller.refresh,
                            child: const Text('Retry'),
                          ),
                        ],
                      ),
                    ),
                  if (shifts.isEmpty)
                    SliverToBoxAdapter(child: _buildEmptyState())
                  else ...[
                    SliverPadding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      sliver: SliverList.builder(
                        itemCount: shifts.length,
                        itemBuilder: (context, index) =>
                            _buildShift(shifts, index),
                      ),
                    ),
                    SliverToBoxAdapter(child: _buildFooter(shifts.length)),
                  ],
                  const SliverToBoxAdapter(child: SizedBox(height: 24)),
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _buildFilters() {
    final from = _controller.fromDate;
    final to = _controller.toDate;
    final dateLabel = from != null && to != null
        ? '${ShiftDisplayFormat.calendarDate(from)} – ${ShiftDisplayFormat.calendarDate(to)}'
        : 'Choose a date range';
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Find an open shift in your branch.',
            style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
          const SizedBox(height: 14),
          AvailableShiftFilters(
            selected: _controller.filter,
            dateLabel: dateLabel,
            onSelected: _controller.selectFilter,
            onChooseDates: _chooseDates,
          ),
          const SizedBox(height: 20),
          Wrap(
            spacing: 12,
            runSpacing: 6,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Text(
                _controller.isLoading && _controller.shifts.isEmpty
                    ? 'Finding shifts…'
                    : _controller.errorMessage != null &&
                          _controller.shifts.isEmpty
                    ? 'Available shifts'
                    : '${_controller.totalItems} ${_controller.totalItems == 1 ? 'shift' : 'shifts'} available',
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
              const Text(
                'Vietnam time · UTC+7',
                style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyState() {
    if (_controller.isLoading) {
      return const Padding(
        padding: EdgeInsets.all(64),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_controller.errorMessage != null) {
      return AppStatePanel(
        icon: Icons.cloud_off_outlined,
        title: 'Couldn’t load shifts',
        message: _controller.errorMessage!,
        actionLabel: 'Try again',
        onAction: _controller.refresh,
        isError: true,
      );
    }
    final hasFilter = _controller.filter != AvailableShiftFilter.allUpcoming;
    return AppStatePanel(
      icon: Icons.event_busy_outlined,
      title: hasFilter ? 'No shifts for these dates' : 'No open shifts yet',
      message: hasFilter
          ? 'Try a different week or view all upcoming dates.'
          : 'New shifts will appear when registration opens. Check back later.',
      actionLabel: hasFilter ? 'Show all dates' : 'Refresh',
      onAction: hasFilter
          ? () => _controller.selectFilter(AvailableShiftFilter.allUpcoming)
          : _controller.refresh,
    );
  }

  Widget _buildShift(List<AvailableShiftModel> shifts, int index) {
    final shift = shifts[index];
    final startsDay =
        index == 0 ||
        !ShiftDisplayFormat.sameDay(
          shifts[index - 1].scheduledStartAt,
          shift.scheduledStartAt,
        );
    return Column(
      key: ValueKey(shift.shiftId),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (startsDay)
          Padding(
            padding: const EdgeInsets.fromLTRB(2, 20, 2, 12),
            child: Row(
              children: [
                const Icon(
                  Icons.calendar_today_outlined,
                  size: 16,
                  color: AppColors.primary,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    ShiftDisplayFormat.dayHeading(shift.scheduledStartAt),
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ),
              ],
            ),
          ),
        AvailableShiftCard(
          shift: shift,
          isOpen: _controller.isOpen(shift),
          isClosingSoon: _controller.isClosingSoon(shift),
        ),
        const SizedBox(height: 12),
      ],
    );
  }

  Widget _buildFooter(int loaded) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
      child: Column(
        children: [
          Text(
            _controller.hasMore
                ? 'Showing $loaded of ${_controller.totalItems} shifts'
                : 'All $loaded ${loaded == 1 ? 'shift' : 'shifts'} shown',
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.textSecondary,
            ),
          ),
          if (_controller.loadMoreError != null) ...[
            const SizedBox(height: 12),
            Text(
              _controller.loadMoreError!,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.error),
            ),
          ],
          if (_controller.hasMore) ...[
            const SizedBox(height: 12),
            AppButton(
              text: _controller.loadMoreError == null
                  ? 'Load more shifts'
                  : 'Try again',
              type: AppButtonType.outline,
              isLoading: _controller.isLoadingMore,
              onPressed: _controller.isLoading ? null : _controller.loadMore,
            ),
          ],
        ],
      ),
    );
  }
}
