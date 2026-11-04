import 'dart:async';
import 'package:flutter/material.dart';
import '../../controllers/shifts/my_shifts_controller.dart';
import '../../models/shifts/available_shift_model.dart';
import '../../services/core/realtime_service.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_state_panel.dart';
import '../../widgets/app_week_calendar.dart';
import 'shift_detail_view.dart';
import 'shift_swap_list_view.dart';
import 'components/cancel_shift_dialog.dart';
import 'components/schedule_timeline_day.dart';
import 'components/shift_display_format.dart';
import '../requests/staff_request_list_view.dart';

class MyShiftsView extends StatefulWidget {
  final MyShiftsController? controller;
  final bool showBackButton;
  const MyShiftsView({super.key, this.controller, this.showBackButton = true});
  @override
  State<MyShiftsView> createState() => _MyShiftsViewState();
}

class _MyShiftsViewState extends State<MyShiftsView>
    with WidgetsBindingObserver {
  late final MyShiftsController _controller;
  bool _handlingCancellation = false;
  final Map<int, String> _reasonDrafts = {};
  final Map<DateTime, GlobalKey> _dayKeys = {};
  final ScrollController _scrollController = ScrollController();

  void _focusDay(DateTime day) {
    _controller.selectDay(day);
    final dayContext = _dayKeys[day]?.currentContext;
    if (dayContext != null) {
      Scrollable.ensureVisible(
        dayContext,
        duration: const Duration(milliseconds: 250),
        alignment: 0.05,
      );
    }
  }

  Future<void> _changeWeek(int offset) async {
    final start = _controller.weekStart;
    await _controller.showWeek(
      DateTime(start.year, start.month, start.day + offset * 7),
    );
    if (mounted && _scrollController.hasClients) _scrollController.jumpTo(0);
  }

  Future<void> _showCurrentWeek() async {
    await _controller.showWeek(_controller.today);
    if (mounted) _focusDay(_controller.today);
  }

  Future<void> _chooseDates() async {
    final today = _controller.today;
    final first = DateTime(today.year - 5, 1, 1);
    final last = DateTime(today.year + 2, 12, 31);
    final from = _controller.fromDate;
    final to = _controller.toDate;
    final range = await showDateRangePicker(
      context: context,
      firstDate: first,
      lastDate: last,
      currentDate: today,
      initialDateRange: to != null && !from.isBefore(first) && !to.isAfter(last)
          ? DateTimeRange(start: from, end: to)
          : null,
      helpText: 'Choose work schedule dates',
      saveText: 'Apply',
    );
    if (!mounted || range == null) return;
    await _controller.selectDateRange(range.start, range.end);
  }

  Timer? _autoSyncTimer;
  StreamSubscription? _realtimeSubscription;

  @override
  void initState() {
    super.initState();
    _controller = widget.controller ?? MyShiftsController();
    WidgetsBinding.instance.addObserver(this);
    _controller.showWeek(_controller.today);

    // 1. Tự động cập nhật lịch khi có thông báo Realtime từ Backend SignalR
    _realtimeSubscription = RealtimeService.instance.onNotification.listen((_) {
      if (mounted) {
        _controller.refresh();
      }
    });

    // 2. Tự động sync ngầm định kỳ (mỗi 6s) để luôn cập nhật ca mới
    _autoSyncTimer = Timer.periodic(const Duration(seconds: 6), (_) {
      if (mounted && !_controller.isLoading) {
        _controller.refresh();
      }
    });
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) _controller.refresh();
  }

  @override
  void dispose() {
    _autoSyncTimer?.cancel();
    _realtimeSubscription?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    _scrollController.dispose();
    if (widget.controller == null) _controller.dispose();
    super.dispose();
  }

  Future<void> _showError() async {
    if (!mounted) return;
    await showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        scrollable: true,
        title: const Text('Cancellation unavailable'),
        content: Text(
          _controller.actionError ?? 'Refresh your shifts and try again.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Close'),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(ctx);
              _controller.refresh();
            },
            child: const Text('Refresh shifts'),
          ),
        ],
      ),
    );
  }

  Future<void> _cancel(AvailableShiftModel shift) async {
    if (_handlingCancellation || !_controller.canCancel(shift)) return;
    _handlingCancellation = true;
    try {
      final latest = await _controller.prepareCancellation(shift);
      if (!mounted) return;
      if (latest == null) {
        await _showError();
        return;
      }
      final reason = await showCancelShiftDialog(
        context,
        latest,
        validateReason: _controller.validateReason,
        initialReason: _reasonDrafts[shift.shiftId] ?? '',
      );
      if (!mounted || reason == null) return;
      _reasonDrafts[shift.shiftId] = reason;
      final success = await _controller.cancel(latest, reason);
      if (!mounted) return;
      if (success) {
        _reasonDrafts.remove(shift.shiftId);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Registration for ${shift.shiftName} cancelled. Your place has been released.',
            ),
          ),
        );
      } else {
        await _showError();
      }
    } finally {
      _handlingCancellation = false;
    }
  }

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: _controller,
    builder: (context, _) {
      final shifts = _controller.shifts;
      final weekDays = _controller.weekDays;
      _dayKeys.removeWhere((date, _) => !weekDays.contains(date));
      final currentWeek = DateTime(
        _controller.today.year,
        _controller.today.month,
        _controller.today.day - _controller.today.weekday + 1,
      );
      final isCurrentWeek = _controller.weekStart == currentWeek;
      return Scaffold(
        appBar: AppBar(
          title: const Text('Work Schedule'),
          automaticallyImplyLeading: widget.showBackButton,
          leading: widget.showBackButton ? const BackButton() : null,
          actions: [
            IconButton(
              tooltip: 'Requests',
              icon: const Icon(Icons.description_outlined),
              onPressed: () async {
                await Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) =>
                        const StaffRequestListView(showBackButton: true),
                  ),
                );
                if (mounted) await _controller.refresh();
              },
            ),
            IconButton(
              tooltip: 'Shift swap requests',
              icon: const Icon(Icons.swap_horiz_rounded),
              onPressed: () async {
                await Navigator.of(context).push(
                  MaterialPageRoute(builder: (_) => const ShiftSwapListView()),
                );
                if (mounted) await _controller.refresh();
              },
            ),
            TextButton(onPressed: _showCurrentWeek, child: const Text('Today')),
            IconButton(
              tooltip: 'Choose dates',
              icon: const Icon(Icons.date_range_outlined),
              onPressed: _chooseDates,
            ),
          ],
        ),
        body: SafeArea(
          top: false,
          child: Column(
            children: [
              if (_controller.isWeekView)
                AppWeekCalendar(
                  title: ShiftDisplayFormat.monthTitle(
                    _controller.weekStart,
                    _controller.weekEnd,
                  ),
                  rangeLabel:
                      '${isCurrentWeek ? 'Current week' : 'Week'} · ${ShiftDisplayFormat.calendarDate(_controller.weekStart)} – ${ShiftDisplayFormat.calendarDate(_controller.weekEnd)}',
                  days: weekDays,
                  selectedDay: _controller.selectedDay,
                  today: _controller.today,
                  markedDays: {
                    for (final day in weekDays)
                      if (_controller.shiftsForDay(day).isNotEmpty) day.day,
                  },
                  onDaySelected: _focusDay,
                  onPreviousWeek: () => _changeWeek(-1),
                  onNextWeek: () => _changeWeek(1),
                )
              else
                Container(
                  width: double.infinity,
                  color: AppColors.surface,
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Text(
                        '${ShiftDisplayFormat.calendarDate(_controller.fromDate)} – ${ShiftDisplayFormat.calendarDate(_controller.toDate ?? _controller.today)}',
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      TextButton(
                        onPressed: _showCurrentWeek,
                        child: const Text('Back to this week'),
                      ),
                    ],
                  ),
                ),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
                color: AppColors.surface,
                child: Wrap(
                  spacing: 16,
                  runSpacing: 8,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    Text(
                      _controller.isLoading && shifts.isEmpty
                          ? 'Loading…'
                          : '${_controller.totalItems} ${_controller.totalItems == 1 ? 'shift' : 'shifts'}',
                      style: const TextStyle(
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const _ScheduleLegend(
                      label: 'Registered',
                      color: AppColors.primary,
                    ),
                    const _ScheduleLegend(
                      label: 'Manager',
                      color: AppColors.warning,
                    ),
                    const _ScheduleLegend(
                      label: 'Done',
                      color: AppColors.success,
                    ),
                    const Text(
                      'UTC+7',
                      style: TextStyle(
                        fontSize: 11,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              if (_controller.isLoading)
                const LinearProgressIndicator(minHeight: 2),
              Expanded(
                child: RefreshIndicator(
                  onRefresh: _controller.refresh,
                  child: SingleChildScrollView(
                    controller: _scrollController,
                    physics: const AlwaysScrollableScrollPhysics(),
                    child: Column(
                      children: [
                        if (_controller.error != null)
                          AppStatePanel(
                            icon: Icons.cloud_off_outlined,
                            title: 'Couldn’t load this schedule',
                            message: _controller.error!,
                            actionLabel: 'Try again',
                            onAction: _controller.refresh,
                            isError: true,
                          ),
                        if (_controller.isWeekView)
                          for (final day in weekDays)
                            _buildDay(
                              day,
                              _controller.shiftsForDay(day),
                              key: _dayKeys.putIfAbsent(day, () => GlobalKey()),
                            )
                        else if (shifts.isEmpty &&
                            !_controller.isLoading &&
                            _controller.error == null)
                          AppStatePanel(
                            icon: Icons.event_available_outlined,
                            title: 'No shifts for these dates',
                            message:
                                'Choose another period to see your work schedule.',
                            actionLabel: 'This week',
                            onAction: _showCurrentWeek,
                          )
                        else
                          for (final day in {
                            for (final shift in shifts) _startDay(shift),
                          })
                            _buildDay(
                              day,
                              shifts
                                  .where((shift) => _startDay(shift) == day)
                                  .toList(),
                            ),
                        if (_controller.hasMore)
                          Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              children: [
                                Text(
                                  'Loaded ${shifts.length} of ${_controller.totalItems} shifts',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                                const SizedBox(height: 8),
                                if (_controller.loadMoreError != null)
                                  Text(
                                    _controller.loadMoreError!,
                                    style: const TextStyle(
                                      color: AppColors.error,
                                    ),
                                  ),
                                AppButton(
                                  text: _controller.loadMoreError == null
                                      ? 'Load more shifts'
                                      : 'Try again',
                                  type: AppButtonType.outline,
                                  isLoading: _controller.isLoadingMore,
                                  onPressed: _controller.isLoading
                                      ? null
                                      : _controller.loadMore,
                                ),
                              ],
                            ),
                          ),
                        const SizedBox(height: 24),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      );
    },
  );

  DateTime _startDay(AvailableShiftModel shift) {
    final date = ShiftDisplayFormat.vietnamTime(shift.scheduledStartAt);
    return DateTime(date.year, date.month, date.day);
  }

  Widget _buildDay(
    DateTime day,
    List<AvailableShiftModel> shifts, {
    Key? key,
  }) => ScheduleTimelineDay(
    key: key,
    date: day,
    selected: day == _controller.selectedDay,
    shifts: shifts,
    emptyLabel: _controller.isLoading
        ? 'Loading schedule…'
        : _controller.error != null
        ? 'Schedule unavailable'
        : _controller.hasMore
        ? 'Load more to check this day.'
        : 'No shifts · Enjoy your day',
    processingId: _controller.processingShiftId,
    canCancel: _controller.canCancel,
    blockedReason: _controller.cancellationBlockedReason,
    onCancel: _cancel,
    onDetails: (shift) async {
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => ShiftDetailView(shiftId: shift.shiftId),
        ),
      );
      if (mounted) await _controller.refresh();
    },
  );
}

class _ScheduleLegend extends StatelessWidget {
  final String label;
  final Color color;
  const _ScheduleLegend({required this.label, required this.color});
  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Container(
        width: 6,
        height: 6,
        decoration: BoxDecoration(color: color, shape: BoxShape.circle),
      ),
      const SizedBox(width: 5),
      Text(
        label,
        style: const TextStyle(fontSize: 11, color: AppColors.textSecondary),
      ),
    ],
  );
}
