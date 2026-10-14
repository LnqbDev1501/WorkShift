import 'package:flutter/material.dart';
import '../../controllers/staff_contract_controller.dart';
import '../../models/staff_contract_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';

/// Màn hình Gửi yêu cầu điều chỉnh thông tin hợp đồng (Submit Employment Contract Change Request)
class SubmitContractChangeRequestView extends StatefulWidget {
  final StaffContractDetailModel contract;
  final StaffContractController controller;

  const SubmitContractChangeRequestView({
    super.key,
    required this.contract,
    required this.controller,
  });

  @override
  State<SubmitContractChangeRequestView> createState() =>
      _SubmitContractChangeRequestViewState();
}

class _FieldOption {
  final String key;
  final String label;
  final String currentValue;
  final IconData icon;

  const _FieldOption({
    required this.key,
    required this.label,
    required this.currentValue,
    required this.icon,
  });
}

class _SubmitContractChangeRequestViewState
    extends State<SubmitContractChangeRequestView> {
  final _formKey = GlobalKey<FormState>();
  final _staffCommentController = TextEditingController();

  late final List<_FieldOption> _availableFields;
  final Set<String> _selectedFieldKeys = {};

  // Controllers cho các trường được chọn
  final Map<String, TextEditingController> _proposedControllers = {};
  final Map<String, TextEditingController> _reasonControllers = {};

  @override
  void initState() {
    super.initState();
    final c = widget.contract;
    _availableFields = [
      _FieldOption(
        key: 'StartDate',
        label: 'Ngày bắt đầu hợp đồng',
        currentValue:
            '${c.startDate.day.toString().padLeft(2, '0')}/${c.startDate.month.toString().padLeft(2, '0')}/${c.startDate.year}',
        icon: Icons.calendar_today_rounded,
      ),
      _FieldOption(
        key: 'EndDate',
        label: 'Ngày kết thúc hợp đồng',
        currentValue: c.endDate != null
            ? '${c.endDate!.day.toString().padLeft(2, '0')}/${c.endDate!.month.toString().padLeft(2, '0')}/${c.endDate!.year}'
            : 'Không thời hạn',
        icon: Icons.event_available_rounded,
      ),
      _FieldOption(
        key: 'EmployeePosition',
        label: 'Vị trí công việc',
        currentValue: c.employeePosition,
        icon: Icons.badge_outlined,
      ),
      _FieldOption(
        key: 'HourlyRate',
        label: 'Mức lương mỗi giờ',
        currentValue: c.formattedHourlyRate,
        icon: Icons.monetization_on_outlined,
      ),
      _FieldOption(
        key: 'ContractTerms',
        label: 'Điều khoản hợp đồng',
        currentValue: c.contractTerms ?? 'Theo quy định công ty',
        icon: Icons.description_outlined,
      ),
    ];
  }

  @override
  void dispose() {
    _staffCommentController.dispose();
    for (var c in _proposedControllers.values) {
      c.dispose();
    }
    for (var c in _reasonControllers.values) {
      c.dispose();
    }
    super.dispose();
  }

  void _toggleField(String key) {
    setState(() {
      if (_selectedFieldKeys.contains(key)) {
        _selectedFieldKeys.remove(key);
        _proposedControllers[key]?.dispose();
        _proposedControllers.remove(key);
        _reasonControllers[key]?.dispose();
        _reasonControllers.remove(key);
      } else {
        _selectedFieldKeys.add(key);
        _proposedControllers[key] = TextEditingController();
        _reasonControllers[key] = TextEditingController();
      }
    });
  }

  Future<void> _handleSubmit() async {
    if (_selectedFieldKeys.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Vui lòng chọn ít nhất một thông tin bạn muốn yêu cầu điều chỉnh.'),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }

    if (!_formKey.currentState!.validate()) return;

    final items = <ContractChangeRequestItemInputModel>[];
    for (var key in _selectedFieldKeys) {
      items.add(ContractChangeRequestItemInputModel(
        fieldName: key,
        proposedValue: _proposedControllers[key]!.text.trim(),
        reason: _reasonControllers[key]?.text.trim(),
      ));
    }

    final success = await widget.controller.submitChangeRequest(
      contractId: widget.contract.contractId,
      staffComment: _staffCommentController.text.trim(),
      items: items,
    );

    if (mounted) {
      if (success) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đã gửi yêu cầu điều chỉnh hợp đồng tới Quản lý thành công!'),
            backgroundColor: AppColors.success,
          ),
        );
        Navigator.pop(context, true);
      } else {
        _showErrorDialog(widget.controller.actionErrorMessage ??
            'Không thể gửi yêu cầu điều chỉnh.');
      }
    }
  }

  void _showErrorDialog(String message) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.error_outline, color: AppColors.error),
            SizedBox(width: 8),
            Text('Lỗi gửi yêu cầu', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          ],
        ),
        content: Text(message, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
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
    final isSubmitting = widget.controller.isSubmittingChangeRequest;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Yêu cầu điều chỉnh hợp đồng',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary, size: 20),
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
              // Thông tin hợp đồng đang điều chỉnh
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFEFF6FF),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: const Color(0xFFBFDBFE)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.info_outline_rounded, color: AppColors.primary, size: 22),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Hợp đồng: ${widget.contract.contractNumber}',
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: AppColors.primaryDark,
                            ),
                          ),
                          const SizedBox(height: 2),
                          const Text(
                            'Hãy chọn các trường thông tin có sai sót để gửi Quản lý xem xét và điều chỉnh lại.',
                            style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // 1. Chọn các trường cần thay đổi
              _buildSectionTitle('1. Chọn thông tin cần điều chỉnh:'),
              ..._availableFields.map((field) {
                final isChecked = _selectedFieldKeys.contains(field.key);
                return Container(
                  margin: const EdgeInsets.only(bottom: 10),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: isChecked ? AppColors.primary : AppColors.borderLight,
                      width: isChecked ? 1.5 : 1,
                    ),
                  ),
                  child: CheckboxListTile(
                    value: isChecked,
                    activeColor: AppColors.primary,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    title: Text(
                      field.label,
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: isChecked ? FontWeight.bold : FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    subtitle: Text(
                      'Hiện tại: ${field.currentValue}',
                      style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                    ),
                    secondary: Icon(field.icon, color: isChecked ? AppColors.primary : AppColors.textMuted, size: 22),
                    onChanged: (_) => _toggleField(field.key),
                  ),
                );
              }),
              const SizedBox(height: 16),

              // 2. Nhập giá trị đề xuất cho các trường đã chọn
              if (_selectedFieldKeys.isNotEmpty) ...[
                _buildSectionTitle('2. Nhập thông tin đề xuất thay đổi:'),
                ..._availableFields
                    .where((f) => _selectedFieldKeys.contains(f.key))
                    .map((f) => _buildFieldInputCard(f)),
                const SizedBox(height: 16),
              ],

              // 3. Ghi chú chung của nhân viên
              _buildSectionTitle('3. Lời nhắn / Ghi chú gửi Quản lý:'),
              TextFormField(
                controller: _staffCommentController,
                maxLines: 3,
                maxLength: 300,
                decoration: InputDecoration(
                  hintText: 'Nhập ghi chú hoặc lý do tổng thể gửi Quản lý (tùy chọn)...',
                  hintStyle: const TextStyle(fontSize: 13, color: AppColors.textMuted),
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
                  contentPadding: const EdgeInsets.all(14),
                ),
              ),
              const SizedBox(height: 24),

              // Nút gửi yêu cầu
              AppButton(
                text: 'Gửi yêu cầu điều chỉnh',
                icon: Icons.send_rounded,
                isLoading: isSubmitting,
                onPressed: isSubmitting ? null : _handleSubmit,
                width: double.infinity,
                height: 50,
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

  Widget _buildFieldInputCard(_FieldOption field) {
    final proposedCtrl = _proposedControllers[field.key]!;
    final reasonCtrl = _reasonControllers[field.key]!;

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(field.icon, size: 18, color: AppColors.primary),
              const SizedBox(width: 8),
              Text(
                field.label,
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          TextFormField(
            controller: proposedCtrl,
            decoration: InputDecoration(
              labelText: 'Giá trị đề xuất mới *',
              hintText: 'Nhập thông tin đúng mà bạn mong muốn',
              labelStyle: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
            ),
            validator: (val) {
              if (val == null || val.trim().isEmpty) {
                return 'Vui lòng nhập giá trị đề xuất mới.';
              }
              return null;
            },
          ),
          const SizedBox(height: 10),
          TextFormField(
            controller: reasonCtrl,
            decoration: InputDecoration(
              labelText: 'Lý do thay đổi',
              hintText: 'Giải thích lý do cần sửa (tùy chọn)',
              labelStyle: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
            ),
          ),
        ],
      ),
    );
  }
}
