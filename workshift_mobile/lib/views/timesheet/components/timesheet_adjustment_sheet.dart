import 'package:flutter/material.dart';
import '../../../models/staff_timesheet_model.dart';
import '../../../theme/app_colors.dart';

class ShiftAdjustmentConfig {
  final StaffShiftAttendanceModel shift;
  bool adjustCheckIn;
  TimeOfDay requestedCheckIn;
  bool adjustCheckOut;
  TimeOfDay requestedCheckOut;
  final TextEditingController reasonController;

  ShiftAdjustmentConfig({
    required this.shift,
    required this.adjustCheckIn,
    required this.requestedCheckIn,
    required this.adjustCheckOut,
    required this.requestedCheckOut,
    required this.reasonController,
  });
}

class TimesheetAdjustmentSheet extends StatefulWidget {
  final List<StaffShiftAttendanceModel> selectedShifts;
  final Future<String?> Function(List<ShiftAdjustmentItemInput> items) onSubmit;
  final int checkInBeforeMinutes;
  final int checkOutAfterMinutes;

  const TimesheetAdjustmentSheet({
    super.key,
    required this.selectedShifts,
    required this.onSubmit,
    this.checkInBeforeMinutes = 30,
    this.checkOutAfterMinutes = 60,
  });

  static Future<bool?> show(
    BuildContext context, {
    required List<StaffShiftAttendanceModel> selectedShifts,
    required Future<String?> Function(List<ShiftAdjustmentItemInput> items) onSubmit,
    int checkInBeforeMinutes = 30,
    int checkOutAfterMinutes = 60,
  }) {
    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => TimesheetAdjustmentSheet(
        selectedShifts: selectedShifts,
        onSubmit: onSubmit,
        checkInBeforeMinutes: checkInBeforeMinutes,
        checkOutAfterMinutes: checkOutAfterMinutes,
      ),
    );
  }

  @override
  State<TimesheetAdjustmentSheet> createState() => _TimesheetAdjustmentSheetState();
}

class _TimesheetAdjustmentSheetState extends State<TimesheetAdjustmentSheet> {
  final List<ShiftAdjustmentConfig> _configs = [];
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    for (final s in widget.selectedShifts) {
      final scheduledStart = s.scheduledStartAt.toLocal();
      final scheduledEnd = s.scheduledEndAt.toLocal();

      final defaultIn = TimeOfDay(hour: scheduledStart.hour, minute: scheduledStart.minute);
      final defaultOut = TimeOfDay(hour: scheduledEnd.hour, minute: scheduledEnd.minute);

      // Default checkbox heuristic:
      // If check-in is missing or late, default adjustCheckIn = true
      // If check-out is missing or early leave, default adjustCheckOut = true
      final needCheckIn = s.checkInTime == null || s.totalLateMinutes > 0;
      final needCheckOut = s.checkOutTime == null || s.totalEarlyLeaveMinutes > 0;

      _configs.add(
        ShiftAdjustmentConfig(
          shift: s,
          adjustCheckIn: needCheckIn || !needCheckOut,
          requestedCheckIn: defaultIn,
          adjustCheckOut: needCheckOut,
          requestedCheckOut: defaultOut,
          reasonController: TextEditingController(),
        ),
      );
    }
  }

  @override
  void dispose() {
    for (final cfg in _configs) {
      cfg.reasonController.dispose();
    }
    super.dispose();
  }

  Future<void> _pickTime({
    required BuildContext context,
    required TimeOfDay initialTime,
    required ValueChanged<TimeOfDay> onSelected,
  }) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: initialTime,
      builder: (context, child) {
        return MediaQuery(
          data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
          child: child ?? const SizedBox.shrink(),
        );
      },
    );

    if (picked != null) {
      onSelected(picked);
      setState(() {});
    }
  }

  String _formatTimeOfDay(TimeOfDay tod) {
    final h = tod.hour.toString().padLeft(2, '0');
    final m = tod.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }

  String _formatDateTimeTime(DateTime dt) {
    final local = dt.toLocal();
    final h = local.hour.toString().padLeft(2, '0');
    final m = local.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }

  DateTime _resolveDateTime(DateTime reference, TimeOfDay tod) {
    final refLocal = reference.toLocal();
    final c1 = DateTime(refLocal.year, refLocal.month, refLocal.day, tod.hour, tod.minute);
    final c2 = c1.subtract(const Duration(days: 1));
    final c3 = c1.add(const Duration(days: 1));

    DateTime best = c1;
    int minDiff = (c1.difference(refLocal).inMinutes).abs();
    if ((c2.difference(refLocal).inMinutes).abs() < minDiff) {
      best = c2;
      minDiff = (c2.difference(refLocal).inMinutes).abs();
    }
    if ((c3.difference(refLocal).inMinutes).abs() < minDiff) {
      best = c3;
    }
    return best.toUtc();
  }

  Future<void> _handleSubmit() async {
    setState(() => _errorMessage = null);

    final List<ShiftAdjustmentItemInput> items = [];
    final nowUtc = DateTime.now().toUtc();

    for (int i = 0; i < _configs.length; i++) {
      final cfg = _configs[i];
      final shift = cfg.shift;

      if (!cfg.adjustCheckIn && !cfg.adjustCheckOut) {
        setState(() {
          _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Please select at least one time adjustment (Check-In or Check-Out).';
        });
        return;
      }

      final reason = cfg.reasonController.text.trim();
      if (reason.isEmpty) {
        setState(() {
          _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Please enter a reason for the adjustment.';
        });
        return;
      }

      final requestedIn = cfg.adjustCheckIn
          ? _resolveDateTime(shift.scheduledStartAt, cfg.requestedCheckIn)
          : null;
      final requestedOut = cfg.adjustCheckOut
          ? _resolveDateTime(shift.scheduledEndAt, cfg.requestedCheckOut)
          : null;

      // 1. Validate Check-in bounds
      if (requestedIn != null) {
        final earliestInUtc = shift.scheduledStartAt.subtract(Duration(minutes: widget.checkInBeforeMinutes));
        if (requestedIn.isBefore(earliestInUtc)) {
          setState(() {
            _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Earliest allowed Check-in is ${_formatDateTimeTime(earliestInUtc)} (${widget.checkInBeforeMinutes}m before shift start).';
          });
          return;
        }

        if (requestedIn.isAfter(nowUtc)) {
          setState(() {
            _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Check-in time cannot be in the future.';
          });
          return;
        }
      }

      // 2. Validate Check-out bounds
      if (requestedOut != null) {
        final latestOutUtc = shift.scheduledEndAt.add(Duration(minutes: widget.checkOutAfterMinutes));
        if (requestedOut.isAfter(latestOutUtc)) {
          setState(() {
            _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Latest allowed Check-out is ${_formatDateTimeTime(latestOutUtc)} (${widget.checkOutAfterMinutes}m after shift end).';
          });
          return;
        }

        if (requestedOut.isAfter(nowUtc)) {
          setState(() {
            _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Check-out time cannot be in the future.';
          });
          return;
        }
      }

      // 3. Cross-validate effective Check-In and Check-Out
      final effectiveIn = requestedIn ?? shift.checkInTime;
      final effectiveOut = requestedOut ?? shift.checkOutTime;

      if (effectiveIn != null && effectiveOut != null) {
        if (!effectiveOut.isAfter(effectiveIn)) {
          setState(() {
            _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Check-in time (${_formatDateTimeTime(effectiveIn)}) must be earlier than Check-out time (${_formatDateTimeTime(effectiveOut)}).';
          });
          return;
        }

        if (effectiveOut.difference(effectiveIn).inMinutes < 15) {
          setState(() {
            _errorMessage = 'Shift #${i + 1} (${shift.shiftName}): Proposed working duration must be at least 15 minutes.';
          });
          return;
        }
      }

      items.add(
        ShiftAdjustmentItemInput(
          assignmentId: shift.assignmentId,
          attendanceId: shift.attendanceId,
          adjustCheckIn: cfg.adjustCheckIn,
          requestedCheckInTime: requestedIn,
          adjustCheckOut: cfg.adjustCheckOut,
          requestedCheckOutTime: requestedOut,
          reason: reason,
        ),
      );
    }

    setState(() => _isSubmitting = true);

    try {
      final error = await widget.onSubmit(items);
      if (mounted) {
        if (error == null) {
          Navigator.pop(context, true);
        } else {
          setState(() {
            _isSubmitting = false;
            _errorMessage = error;
          });
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
          _errorMessage = e.toString();
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;

    return Container(
      constraints: BoxConstraints(
        maxHeight: MediaQuery.of(context).size.height * 0.9,
      ),
      padding: EdgeInsets.only(bottom: bottomInset),
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      child: SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top Drag Handle
            Center(
              child: Container(
                margin: const EdgeInsets.only(top: 10, bottom: 8),
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: AppColors.border,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),

            // Header
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Request Timesheet Adjustment',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          '${_configs.length} shift(s) selected for adjustment',
                          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                        ),
                      ],
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close_rounded),
                    onPressed: () => Navigator.pop(context),
                    color: AppColors.textSecondary,
                  ),
                ],
              ),
            ),

            const Divider(height: 1, color: AppColors.border),

            if (_errorMessage != null)
              Container(
                margin: const EdgeInsets.fromLTRB(20, 12, 20, 0),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.errorBg,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFFCA5A5)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.error_outline_rounded, color: AppColors.error, size: 20),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        _errorMessage!,
                        style: const TextStyle(fontSize: 12, color: AppColors.error, fontWeight: FontWeight.w500),
                      ),
                    ),
                  ],
                ),
              ),

            // Scrollable List of Shift Configs
            Flexible(
              child: ListView.separated(
                shrinkWrap: true,
                padding: const EdgeInsets.all(20),
                itemCount: _configs.length,
                separatorBuilder: (_, _) => const SizedBox(height: 16),
                itemBuilder: (context, index) {
                  return _buildShiftAdjustmentCard(_configs[index], index);
                },
              ),
            ),

            // Bottom Submit Button
            Container(
              padding: const EdgeInsets.fromLTRB(20, 12, 20, 12),
              decoration: const BoxDecoration(
                color: Colors.white,
                border: Border(top: BorderSide(color: AppColors.border)),
              ),
              child: SizedBox(
                width: double.infinity,
                height: 48,
                child: ElevatedButton.icon(
                  onPressed: _isSubmitting ? null : _handleSubmit,
                  icon: _isSubmitting
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Icon(Icons.send_rounded, size: 18),
                  label: Text(
                    _isSubmitting
                        ? 'Submitting Request...'
                        : 'Submit Adjustment (${_configs.length} Shifts)',
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    elevation: 0,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildShiftAdjustmentCard(ShiftAdjustmentConfig cfg, int index) {
    final shift = cfg.shift;

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header: Shift Name & Date
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Shift #${index + 1}: ${shift.shiftName}',
                      style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${shift.dateDisplay} (${shift.scheduledRangeDisplay})',
                      style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: AppColors.primaryLight,
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  shift.attendanceStatus,
                  style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: AppColors.primary),
                ),
              ),
            ],
          ),

          const SizedBox(height: 8),

          // Current Recorded Times
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: AppColors.borderLight),
            ),
            child: Row(
              children: [
                const Icon(Icons.history_rounded, size: 14, color: AppColors.textMuted),
                const SizedBox(width: 6),
                Text(
                  'Recorded: Check-in: ${shift.checkInDisplay} • Check-out: ${shift.checkOutDisplay}',
                  style: const TextStyle(fontSize: 11, color: AppColors.textSecondary),
                ),
              ],
            ),
          ),

          const Divider(height: 18, color: AppColors.border),

          // Check-in Adjust Checkbox & Picker
          Row(
            children: [
              Checkbox(
                value: cfg.adjustCheckIn,
                activeColor: AppColors.primary,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
                onChanged: (val) {
                  setState(() => cfg.adjustCheckIn = val ?? false);
                },
              ),
              const Text(
                'Adjust Check-In:',
                style: TextStyle(fontSize: 13, fontWeight: FontWeight.w500, color: AppColors.textPrimary),
              ),
              const Spacer(),
              if (cfg.adjustCheckIn)
                InkWell(
                  onTap: () {
                    _pickTime(
                      context: context,
                      initialTime: cfg.requestedCheckIn,
                      onSelected: (time) => cfg.requestedCheckIn = time,
                    );
                  },
                  borderRadius: BorderRadius.circular(8),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: AppColors.primary),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.access_time_rounded, size: 14, color: AppColors.primary),
                        const SizedBox(width: 4),
                        Text(
                          _formatTimeOfDay(cfg.requestedCheckIn),
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
                )
              else
                const Text('Keep current', style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
            ],
          ),

          if (cfg.adjustCheckIn)
            Padding(
              padding: const EdgeInsets.only(left: 48, bottom: 6),
              child: Text(
                'Earliest allowed: ${_formatDateTimeTime(shift.scheduledStartAt.subtract(Duration(minutes: widget.checkInBeforeMinutes)))} (${widget.checkInBeforeMinutes}m before shift)',
                style: const TextStyle(fontSize: 11, color: AppColors.textMuted, fontStyle: FontStyle.italic),
              ),
            ),

          // Check-out Adjust Checkbox & Picker
          Row(
            children: [
              Checkbox(
                value: cfg.adjustCheckOut,
                activeColor: AppColors.primary,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
                onChanged: (val) {
                  setState(() => cfg.adjustCheckOut = val ?? false);
                },
              ),
              const Text(
                'Adjust Check-Out:',
                style: TextStyle(fontSize: 13, fontWeight: FontWeight.w500, color: AppColors.textPrimary),
              ),
              const Spacer(),
              if (cfg.adjustCheckOut)
                InkWell(
                  onTap: () {
                    _pickTime(
                      context: context,
                      initialTime: cfg.requestedCheckOut,
                      onSelected: (time) => cfg.requestedCheckOut = time,
                    );
                  },
                  borderRadius: BorderRadius.circular(8),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: AppColors.primary),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.access_time_rounded, size: 14, color: AppColors.primary),
                        const SizedBox(width: 4),
                        Text(
                          _formatTimeOfDay(cfg.requestedCheckOut),
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
                )
              else
                const Text('Keep current', style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
            ],
          ),

          if (cfg.adjustCheckOut)
            Padding(
              padding: const EdgeInsets.only(left: 48, bottom: 6),
              child: Text(
                'Latest allowed: ${_formatDateTimeTime(shift.scheduledEndAt.add(Duration(minutes: widget.checkOutAfterMinutes)))} (${widget.checkOutAfterMinutes}m after shift)',
                style: const TextStyle(fontSize: 11, color: AppColors.textMuted, fontStyle: FontStyle.italic),
              ),
            ),

          const SizedBox(height: 8),

          // Reason TextField
          TextField(
            controller: cfg.reasonController,
            decoration: InputDecoration(
              hintText: 'Reason for adjustment (e.g., Facial scan failed, forgot checkout)...',
              hintStyle: const TextStyle(fontSize: 12, color: AppColors.textMuted),
              filled: true,
              fillColor: Colors.white,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: AppColors.border),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: AppColors.border),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
              ),
              contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            ),
            maxLines: 2,
            style: const TextStyle(fontSize: 13),
          ),
        ],
      ),
    );
  }
}
