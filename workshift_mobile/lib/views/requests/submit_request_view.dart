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
          ? 'CHỌN GIỜ DỰ KIẾN ĐẾN'
          : 'CHỌN GIỜ DỰ KIẾN VỀ',
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
      return 'Vui lòng chọn ca làm việc áp dụng trước.';
    }
    if (_selectedAdjustmentTime == null) {
      return _selectedRequestType == 'LATE_ARRIVAL'
          ? 'Vui lòng chọn giờ dự kiến đến làm.'
          : 'Vui lòng chọn giờ dự kiến về sớm.';
    }

    final reqTime = _calculateRequestedDateTime();
    if (reqTime == null) return null;

    final shiftStart = _selectedShift!.scheduledStartAt.toLocal();
    final shiftEnd = _selectedShift!.scheduledEndAt.toLocal();

    // Backend rule: requestedTimeUtc > shift.ScheduledStartAt && requestedTimeUtc < shift.ScheduledEndAt
    if (!reqTime.isAfter(shiftStart) || !reqTime.isBefore(shiftEnd)) {
      final isLate = _selectedRequestType == 'LATE_ARRIVAL';
      return isLate
          ? 'Giờ đến (${_formatTime(reqTime)}) không hợp lệ! Phải sau giờ bắt đầu (${_formatTime(shiftStart)}) và trước giờ kết thúc (${_formatTime(shiftEnd)}).'
          : 'Giờ về (${_formatTime(reqTime)}) không hợp lệ! Phải sau giờ bắt đầu (${_formatTime(shiftStart)}) và trước giờ kết thúc (${_formatTime(shiftEnd)}).';
    }

    return null;
  }

  Future<void> _handleSubmit() async {
    if (!_formKey.currentState!.validate()) return;

    if (_selectedRequestType == 'LEAVE') {
      if (_selectedLeaveAssignmentIds.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Vui lòng chọn ít nhất 1 ca làm việc để xin nghỉ.'),
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
            content: Text('Gửi đơn xin nghỉ phép thành công!'),
            backgroundColor: AppColors.success,
          ),
        );
        Navigator.pop(context, true);
      } else if (mounted) {
        _showErrorDialog(widget.controller.actionErrorMessage ?? 'Gửi đơn nghỉ phép thất bại');
      }
    } else {
      // LATE_ARRIVAL hoặc EARLY_LEAVE
      if (_selectedTimeAdjustmentAssignmentId == null || _selectedShift == null) {
        _showErrorDialog('Vui lòng chọn ca làm việc áp dụng trước khi gửi đơn.');
        return;
      }

      if (_selectedAdjustmentTime == null) {
        _showErrorDialog(
          _selectedRequestType == 'LATE_ARRIVAL'
              ? 'Vui lòng chọn giờ dự kiến đến làm.'
              : 'Vui lòng chọn giờ dự kiến về sớm.',
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
                  ? 'Gửi đơn xin đi trễ thành công!'
                  : 'Gửi đơn xin về sớm thành công!',
            ),
            backgroundColor: AppColors.success,
          ),
        );
        Navigator.pop(context, true);
      } else if (mounted) {
        _showErrorDialog(widget.controller.actionErrorMessage ?? 'Gửi đơn thất bại');
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
            Text('Không thể gửi yêu cầu', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
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
            child: const Text('Đã hiểu'),
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
          'Tạo yêu cầu mới',
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
              _buildSectionTitle('1. Chọn loại yêu cầu'),
              Container(
                decoration: BoxDecoration(
                  color: const Color(0xFFF1F5F9),
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.all(4),
                child: Row(
                  children: [
                    _buildTypeTab('LEAVE', 'Nghỉ phép', Icons.event_busy_rounded),
                    _buildTypeTab('LATE_ARRIVAL', 'Đi trễ', Icons.more_time_rounded),
                    _buildTypeTab('EARLY_LEAVE', 'Về sớm', Icons.timer_off_rounded),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // 2. Danh sách ca làm việc đủ điều kiện
              _buildSectionTitle(
                _selectedRequestType == 'LEAVE'
                    ? '2. Chọn ca muốn nghỉ (có thể chọn nhiều ca)'
                    : '2. Chọn ca làm việc áp dụng',
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
                        'Không có ca làm việc nào đủ điều kiện',
                        style: TextStyle(fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        _selectedRequestType == 'LEAVE'
                            ? 'Bạn chỉ có thể xin nghỉ các ca làm việc trong tương lai chưa bắt đầu.'
                            : 'Không tìm thấy ca làm việc phù hợp cho yêu cầu này.',
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
                      ? '3. Giờ dự kiến đến làm'
                      : '3. Giờ dự kiến về sớm',
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
                                'Ca: ${_selectedShift!.shiftName} (${_formatTime(_selectedShift!.scheduledStartAt.toLocal())} - ${_formatTime(_selectedShift!.scheduledEndAt.toLocal())}, ${_formatDate(_selectedShift!.scheduledStartAt.toLocal())})',
                                style: const TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFF1E40AF),
                                ),
                              ),
                              const SizedBox(height: 3),
                              Text(
                                _selectedRequestType == 'LATE_ARRIVAL'
                                    ? 'Thời gian đến phải lớn hơn ${_formatTime(_selectedShift!.scheduledStartAt.toLocal())} và nhỏ hơn ${_formatTime(_selectedShift!.scheduledEndAt.toLocal())}.'
                                    : 'Thời gian về phải lớn hơn ${_formatTime(_selectedShift!.scheduledStartAt.toLocal())} và nhỏ hơn ${_formatTime(_selectedShift!.scheduledEndAt.toLocal())}.',
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
                            'Vui lòng tích chọn một ca làm việc ở mục 2 phía trên trước.',
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
                                      : 'Nhấn để chọn thời gian',
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
                _selectedRequestType == 'LEAVE' ? '3. Lý do xin nghỉ' : '4. Lý do',
              ),
              TextFormField(
                controller: _reasonController,
                maxLines: 4,
                maxLength: 500,
                decoration: InputDecoration(
                  hintText: 'Nhập chi tiết lý do gửi yêu cầu (bắt buộc)...',
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
                    return 'Vui lòng nhập lý do gửi yêu cầu.';
                  }
                  if (val.trim().length > 500) {
                    return 'Lý do không được vượt quá 500 ký tự.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 24),

              // 5. Nút gửi
              AppButton(
                text: 'Gửi yêu cầu',
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
