import 'package:flutter/material.dart';
import '../../controllers/staff_request_controller.dart';
import '../../models/staff_request_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';

/// Màn hình Tạo và gửi yêu cầu mới của Staff (Submit Employee Request)
class SubmitRequestView extends StatefulWidget {
  final StaffRequestController controller;
  final String initialRequestType;

  const SubmitRequestView({
    super.key,
    required this.controller,
    this.initialRequestType = 'LEAVE',
  });

  @override
  State<SubmitRequestView> createState() => _SubmitRequestViewState();
}

class _SubmitRequestViewState extends State<SubmitRequestView> {
  late String _selectedRequestType; // LEAVE, LATE_ARRIVAL, EARLY_LEAVE
  final _formKey = GlobalKey<FormState>();
  final _reasonController = TextEditingController();

  // Với LEAVE: Danh sách ca đã chọn (nhiều ca)
  final Set<int> _selectedLeaveAssignmentIds = {};

  // Với LATE_ARRIVAL hoặc EARLY_LEAVE: 1 ca duy nhất
  int? _selectedTimeAdjustmentAssignmentId;
  StaffEligibleShiftModel? _selectedShift;
  TimeOfDay? _selectedAdjustmentTime;

  @override
  void initState() {
    super.initState();
    _selectedRequestType = widget.initialRequestType;
    widget.controller.addListener(_onControllerUpdate);
    _loadEligibleShifts();
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerUpdate);
    _reasonController.dispose();
    super.dispose();
  }

  void _onControllerUpdate() {
    if (mounted) setState(() {});
  }

  void _loadEligibleShifts() {
    _selectedLeaveAssignmentIds.clear();
    _selectedTimeAdjustmentAssignmentId = null;
    _selectedShift = null;
    _selectedAdjustmentTime = null;
    widget.controller.loadEligibleShifts(_selectedRequestType);
  }

  void _onTypeChanged(String newType) {
    if (_selectedRequestType == newType) return;
    setState(() {
      _selectedRequestType = newType;
    });
    _loadEligibleShifts();
  }

  String _formatTime(DateTime dt) {
    final local = dt.toLocal();
    final h = local.hour.toString().padLeft(2, '0');
    final m = local.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    final d = local.day.toString().padLeft(2, '0');
    final m = local.month.toString().padLeft(2, '0');
    final y = local.year.toString();
    return '$d/$m/$y';
  }

  Future<void> _pickTime() async {
    final initial = _selectedAdjustmentTime ??
        (_selectedShift != null
            ? TimeOfDay.fromDateTime(_selectedShift!.scheduledStartAt.toLocal())
            : TimeOfDay.now());

    final picked = await showTimePicker(
      context: context,
      initialTime: initial,
      helpText: _selectedRequestType == 'LATE_ARRIVAL'
          ? 'SELECT EXPECTED ARRIVAL TIME'
          : 'SELECT EXPECTED DEPARTURE TIME',
    );

    if (picked != null) {
      setState(() {
        _selectedAdjustmentTime = picked;
      });
    }
  }

  DateTime? _calculateRequestedDateTime() {
    if (_selectedShift == null || _selectedAdjustmentTime == null) return null;
    final shiftStart = _selectedShift!.scheduledStartAt.toLocal();
    final shiftEnd = _selectedShift!.scheduledEndAt.toLocal();

    // Nếu ca làm việc kéo dài qua ngày hôm sau (overnight shift)
    if (shiftEnd.day != shiftStart.day) {
      if (_selectedAdjustmentTime!.hour < shiftStart.hour) {
        return DateTime(
          shiftEnd.year,
          shiftEnd.month,
          shiftEnd.day,
          _selectedAdjustmentTime!.hour,
          _selectedAdjustmentTime!.minute,
        );
      }
    }

    return DateTime(
      shiftStart.year,
      shiftStart.month,
      shiftStart.day,
      _selectedAdjustmentTime!.hour,
      _selectedAdjustmentTime!.minute,
    );
  }

  String? _getAdjustmentTimeValidationError() {
    if (_selectedRequestType == 'LEAVE') return null;
    if (_selectedShift == null) {
      return 'Please select an applicable shift first.';
    }
    if (_selectedAdjustmentTime == null) {
      return _selectedRequestType == 'LATE_ARRIVAL'
          ? 'Please select your expected arrival time.'
          : 'Please select your expected departure time.';
    }

    final reqTime = _calculateRequestedDateTime();
    if (reqTime == null) return null;

    final shiftStart = _selectedShift!.scheduledStartAt.toLocal();
    final shiftEnd = _selectedShift!.scheduledEndAt.toLocal();

    // Backend rule: requestedTimeUtc > shift.ScheduledStartAt && requestedTimeUtc < shift.ScheduledEndAt
    if (!reqTime.isAfter(shiftStart) || !reqTime.isBefore(shiftEnd)) {
      final isLate = _selectedRequestType == 'LATE_ARRIVAL';
      return isLate
          ? 'Arrival time (${_formatTime(reqTime)}) is invalid! It must be after shift start (${_formatTime(shiftStart)}) and before shift end (${_formatTime(shiftEnd)}).'
          : 'Departure time (${_formatTime(reqTime)}) is invalid! It must be after shift start (${_formatTime(shiftStart)}) and before shift end (${_formatTime(shiftEnd)}).';
    }

    return null;
  }

  Future<void> _handleSubmit() async {
    if (!_formKey.currentState!.validate()) return;

    if (_selectedRequestType == 'LEAVE') {
      if (_selectedLeaveAssignmentIds.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Please select at least one shift to request leave.'),
            backgroundColor: AppColors.error,
          ),
        );
        return;
      }

      final res = await widget.controller.submitLeaveRequest(
        assignmentIds: _selectedLeaveAssignmentIds.toList(),
        reason: _reasonController.text,
      );

      if (mounted && res != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Leave request submitted successfully!'),
            backgroundColor: AppColors.success,
          ),
        );
        Navigator.pop(context, true);
      } else if (mounted) {
        _showErrorDialog(widget.controller.actionErrorMessage ?? 'Failed to submit leave request');
      }
    } else {
      // LATE_ARRIVAL hoặc EARLY_LEAVE
      if (_selectedTimeAdjustmentAssignmentId == null || _selectedShift == null) {
        _showErrorDialog('Please select an applicable shift before submitting.');
        return;
      }

      if (_selectedAdjustmentTime == null) {
        _showErrorDialog(
          _selectedRequestType == 'LATE_ARRIVAL'
              ? 'Please select your expected arrival time.'
              : 'Please select your expected departure time.',
        );
        return;
      }

      final validationError = _getAdjustmentTimeValidationError();
      if (validationError != null) {
        _showErrorDialog(validationError);
        return;
      }

      final requestedDateTime = _calculateRequestedDateTime()!;

      StaffEmployeeRequestDetailModel? res;
      if (_selectedRequestType == 'LATE_ARRIVAL') {
        res = await widget.controller.submitLateArrivalRequest(
          assignmentId: _selectedTimeAdjustmentAssignmentId!,
          requestedTime: requestedDateTime,
          reason: _reasonController.text,
        );
      } else {
        res = await widget.controller.submitEarlyLeaveRequest(
          assignmentId: _selectedTimeAdjustmentAssignmentId!,
          requestedTime: requestedDateTime,
          reason: _reasonController.text,
        );
      }

      if (mounted && res != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              _selectedRequestType == 'LATE_ARRIVAL'
                  ? 'Late arrival request submitted successfully!'
                  : 'Early departure request submitted successfully!',
            ),
            backgroundColor: AppColors.success,
          ),
        );
        Navigator.pop(context, true);
      } else if (mounted) {
        _showErrorDialog(widget.controller.actionErrorMessage ?? 'Failed to submit request');
      }
    }
  }

  void _showErrorDialog(String message) {
    var displayMsg = message;
    if (displayMsg.startsWith('Exception: ')) {
      displayMsg = displayMsg.substring('Exception: '.length).trim();
    }
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.error_outline, color: AppColors.error),
            SizedBox(width: 8),
            Text('Cannot Submit Request', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          ],
        ),
        content: Text(displayMsg, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary, height: 1.4)),
        actions: [
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final eligibleShifts = widget.controller.eligibleShifts;
    final isLoadingShifts = widget.controller.isLoadingEligibleShifts;
    final isSubmitting = widget.controller.isSubmitting;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'New Request',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(Icons.close_rounded, color: AppColors.textPrimary, size: 22),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: Form(
        key: _formKey,
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // 1. Selector chọn Loại yêu cầu
              _buildSectionTitle('1. Select Request Type'),
              Container(
                decoration: BoxDecoration(
                  color: const Color(0xFFF1F5F9),
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.all(4),
                child: Row(
                  children: [
                    _buildTypeTab('LEAVE', 'Leave', Icons.event_busy_rounded),
                    _buildTypeTab('LATE_ARRIVAL', 'Late Arrival', Icons.more_time_rounded),
                    _buildTypeTab('EARLY_LEAVE', 'Early Departure', Icons.timer_off_rounded),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // 2. Danh sách ca làm việc đủ điều kiện
              _buildSectionTitle(
                _selectedRequestType == 'LEAVE'
                    ? '2. Select shifts to take leave (multiple choices allowed)'
                    : '2. Select applicable shift',
              ),
              if (isLoadingShifts)
                const Center(
                  child: Padding(
                    padding: EdgeInsets.symmetric(vertical: 24),
                    child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
                  ),
                )
              else if (eligibleShifts.isEmpty)
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: AppColors.borderLight),
                  ),
                  child: Column(
                    children: [
                      const Icon(Icons.calendar_month_outlined, size: 36, color: AppColors.textMuted),
                      const SizedBox(height: 8),
                      const Text(
                        'No eligible shifts found',
                        style: TextStyle(fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        _selectedRequestType == 'LEAVE'
                            ? 'You can only request leave for upcoming shifts that have not started yet.'
                            : 'No eligible shifts found for this request.',
                        textAlign: TextAlign.center,
                        style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                      ),
                    ],
                  ),
                )
              else
                ...eligibleShifts.map((shift) {
                  if (_selectedRequestType == 'LEAVE') {
                    final isChecked = _selectedLeaveAssignmentIds.contains(shift.assignmentId);
                    return _buildShiftCheckboxItem(shift, isChecked);
                  } else {
                    final isSelected = _selectedTimeAdjustmentAssignmentId == shift.assignmentId;
                    return _buildShiftRadioItem(shift, isSelected);
                  }
                }),
              const SizedBox(height: 20),

              // 3. Chọn giờ nếu là Đi trễ / Về sớm
              if (_selectedRequestType != 'LEAVE') ...[
                _buildSectionTitle(
                  _selectedRequestType == 'LATE_ARRIVAL'
                      ? '3. Expected arrival time'
                      : '3. Expected departure time',
                ),
                if (_selectedShift != null) ...[
                  Container(
                    margin: const EdgeInsets.only(bottom: 12),
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFEFF6FF),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFBFDBFE)),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(Icons.info_outline_rounded, color: AppColors.primary, size: 20),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Shift: ${_selectedShift!.shiftName} (${_formatTime(_selectedShift!.scheduledStartAt.toLocal())} - ${_formatTime(_selectedShift!.scheduledEndAt.toLocal())}, ${_formatDate(_selectedShift!.scheduledStartAt.toLocal())})',
                                style: const TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFF1E40AF),
                                ),
                              ),
                              const SizedBox(height: 3),
                              Text(
                                _selectedRequestType == 'LATE_ARRIVAL'
                                    ? 'Arrival time must be after ${_formatTime(_selectedShift!.scheduledStartAt.toLocal())} and before ${_formatTime(_selectedShift!.scheduledEndAt.toLocal())}.'
                                    : 'Departure time must be after ${_formatTime(_selectedShift!.scheduledStartAt.toLocal())} and before ${_formatTime(_selectedShift!.scheduledEndAt.toLocal())}.',
                                style: const TextStyle(fontSize: 12, color: Color(0xFF1E3A8A)),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ] else ...[
                  Container(
                    margin: const EdgeInsets.only(bottom: 12),
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFFBEB),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFFDE68A)),
                    ),
                    child: const Row(
                      children: [
                        Icon(Icons.warning_amber_rounded, color: Color(0xFFD97706), size: 20),
                        SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            'Please select an applicable shift in step 2 above first.',
                            style: TextStyle(fontSize: 12, color: Color(0xFF92400E)),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                Builder(
                  builder: (context) {
                    final timeError = _getAdjustmentTimeValidationError();
                    final hasError = timeError != null && _selectedAdjustmentTime != null;
                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        GestureDetector(
                          onTap: _pickTime,
                          child: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                            decoration: BoxDecoration(
                              color: hasError ? const Color(0xFFFEF2F2) : Colors.white,
                              borderRadius: BorderRadius.circular(14),
                              border: Border.all(
                                color: hasError
                                    ? AppColors.error
                                    : (_selectedAdjustmentTime != null
                                        ? AppColors.primary
                                        : AppColors.borderLight),
                                width: hasError ? 1.5 : 1.0,
                              ),
                            ),
                            child: Row(
                              children: [
                                Icon(
                                  Icons.access_time_rounded,
                                  color: hasError
                                      ? AppColors.error
                                      : (_selectedAdjustmentTime != null
                                          ? AppColors.primary
                                          : AppColors.textMuted),
                                  size: 22,
                                ),
                                const SizedBox(width: 12),
                                Text(
                                  _selectedAdjustmentTime != null
                                      ? _selectedAdjustmentTime!.format(context)
                                      : 'Tap to select time',
                                  style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: _selectedAdjustmentTime != null
                                        ? FontWeight.bold
                                        : FontWeight.normal,
                                    color: hasError
                                        ? AppColors.error
                                        : (_selectedAdjustmentTime != null
                                            ? AppColors.textPrimary
                                            : AppColors.textMuted),
                                  ),
                                ),
                                const Spacer(),
                                const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
                              ],
                            ),
                          ),
                        ),
                        if (hasError)
                          Container(
                            margin: const EdgeInsets.only(top: 8),
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                            decoration: BoxDecoration(
                              color: const Color(0xFFFEE2E2),
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(color: const Color(0xFFFCA5A5)),
                            ),
                            child: Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Padding(
                                  padding: EdgeInsets.only(top: 2),
                                  child: Icon(Icons.error_outline, color: AppColors.error, size: 16),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    timeError,
                                    style: const TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                      color: Color(0xFF991B1B),
                                      height: 1.3,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                      ],
                    );
                  },
                ),
                const SizedBox(height: 20),
              ],

              // 4. Nhập lý do
              _buildSectionTitle(
                _selectedRequestType == 'LEAVE' ? '3. Reason for leave' : '4. Reason',
              ),
              TextFormField(
                controller: _reasonController,
                maxLines: 4,
                maxLength: 500,
                decoration: InputDecoration(
                  hintText: 'Enter detailed reason for the request (required)...',
                  hintStyle: const TextStyle(fontSize: 14, color: AppColors.textMuted),
                  filled: true,
                  fillColor: Colors.white,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: const BorderSide(color: AppColors.borderLight),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: const BorderSide(color: AppColors.borderLight),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
                  ),
                  errorBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: const BorderSide(color: AppColors.error),
                  ),
                  contentPadding: const EdgeInsets.all(14),
                ),
                validator: (val) {
                  if (val == null || val.trim().isEmpty) {
                    return 'Please enter a reason for the request.';
                  }
                  if (val.trim().length > 500) {
                    return 'Reason cannot exceed 500 characters.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 24),

              // 5. Nút gửi
              AppButton(
                text: 'Submit Request',
                isLoading: isSubmitting,
                onPressed: isSubmitting ? null : _handleSubmit,
                icon: Icons.send_rounded,
                width: double.infinity,
                height: 52,
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8, left: 4),
      child: Text(
        title,
        style: const TextStyle(
          fontSize: 15,
          fontWeight: FontWeight.bold,
          color: AppColors.textPrimary,
        ),
      ),
    );
  }

  Widget _buildTypeTab(String type, String label, IconData icon) {
    final isSelected = _selectedRequestType == type;
    return Expanded(
      child: GestureDetector(
        onTap: () => _onTypeChanged(type),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.symmetric(vertical: 10),
          decoration: BoxDecoration(
            color: isSelected ? Colors.white : Colors.transparent,
            borderRadius: BorderRadius.circular(10),
            boxShadow: isSelected
                ? [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.06),
                      blurRadius: 4,
                      offset: const Offset(0, 2),
                    ),
                  ]
                : null,
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                icon,
                size: 16,
                color: isSelected ? AppColors.primary : AppColors.textSecondary,
              ),
              const SizedBox(width: 6),
              Text(
                label,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                  color: isSelected ? AppColors.primary : AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildShiftCheckboxItem(StaffEligibleShiftModel shift, bool isChecked) {
    return GestureDetector(
      onTap: () {
        setState(() {
          if (isChecked) {
            _selectedLeaveAssignmentIds.remove(shift.assignmentId);
          } else {
            _selectedLeaveAssignmentIds.add(shift.assignmentId);
          }
        });
      },
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: isChecked ? const Color(0xFFEFF6FF) : Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: isChecked ? AppColors.primary : AppColors.borderLight,
            width: isChecked ? 1.5 : 1.0,
          ),
        ),
        child: Row(
          children: [
            Checkbox(
              value: isChecked,
              activeColor: AppColors.primary,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
              onChanged: (val) {
                setState(() {
                  if (val == true) {
                    _selectedLeaveAssignmentIds.add(shift.assignmentId);
                  } else {
                    _selectedLeaveAssignmentIds.remove(shift.assignmentId);
                  }
                });
              },
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    shift.shiftName,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    '${_formatDate(shift.scheduledStartAt)} • ${_formatTime(shift.scheduledStartAt)} - ${_formatTime(shift.scheduledEndAt)}',
                    style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                  ),
                  Text(
                    shift.branchName,
                    style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildShiftRadioItem(StaffEligibleShiftModel shift, bool isSelected) {
    return GestureDetector(
      onTap: () {
        setState(() {
          _selectedTimeAdjustmentAssignmentId = shift.assignmentId;
          _selectedShift = shift;
          // Thiết lập giờ mặc định: đi trễ là start + 30m, về sớm là end - 30m
          final base = _selectedRequestType == 'LATE_ARRIVAL'
              ? shift.scheduledStartAt.toLocal().add(const Duration(minutes: 30))
              : shift.scheduledEndAt.toLocal().subtract(const Duration(minutes: 30));
          _selectedAdjustmentTime = TimeOfDay.fromDateTime(base);
        });
      },
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: isSelected ? const Color(0xFFEFF6FF) : Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.borderLight,
            width: isSelected ? 1.5 : 1.0,
          ),
        ),
        child: Row(
          children: [
            Container(
              margin: const EdgeInsets.symmetric(horizontal: 4),
              width: 20,
              height: 20,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(
                  color: isSelected ? AppColors.primary : AppColors.textMuted,
                  width: isSelected ? 6 : 2,
                ),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    shift.shiftName,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    '${_formatDate(shift.scheduledStartAt)} • ${_formatTime(shift.scheduledStartAt)} - ${_formatTime(shift.scheduledEndAt)}',
                    style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                  ),
                  Text(
                    shift.branchName,
                    style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
