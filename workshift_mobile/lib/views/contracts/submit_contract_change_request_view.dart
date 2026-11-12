import 'package:flutter/material.dart';
import '../../controllers/contracts/staff_contract_controller.dart';
import '../../models/contracts/staff_contract_model.dart';
import '../../theme/app_colors.dart';
import '../../utils/format_utils.dart';
import '../../widgets/app_button.dart';

/// Screen: Submit Employment Contract Change Request
/// Allows staff to propose adjustments to their employment contract to their manager.
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

  final Map<String, TextEditingController> _proposedControllers = {};
  final Map<String, TextEditingController> _reasonControllers = {};

  @override
  void initState() {
    super.initState();
    final c = widget.contract;
    _availableFields = [
      _FieldOption(
        key: 'EmployeePosition',
        label: 'Position / Title',
        currentValue: c.employeePosition,
        icon: Icons.badge_outlined,
      ),
      _FieldOption(
        key: 'HourlyRate',
        label: 'Base Hourly Rate',
        currentValue: c.regionalMinimumWage != null && c.regionalMinimumWage! > 0
            ? '${c.formattedHourlyRate} (Min: ${formatCurrency(c.regionalMinimumWage!)}/hr)'
            : c.formattedHourlyRate,
        icon: Icons.monetization_on_outlined,
      ),
      _FieldOption(
        key: 'StartDate',
        label: 'Contract Start Date',
        currentValue:
            '${c.startDate.day.toString().padLeft(2, '0')}/${c.startDate.month.toString().padLeft(2, '0')}/${c.startDate.year}',
        icon: Icons.calendar_today_rounded,
      ),
      _FieldOption(
        key: 'EndDate',
        label: 'Contract End Date',
        currentValue: c.endDate != null
            ? '${c.endDate!.day.toString().padLeft(2, '0')}/${c.endDate!.month.toString().padLeft(2, '0')}/${c.endDate!.year}'
            : 'Indefinite-term',
        icon: Icons.event_available_rounded,
      ),
      _FieldOption(
        key: 'ContractTerms',
        label: 'Contract Terms',
        currentValue: c.contractTerms ?? 'Standard workplace policy',
        icon: Icons.description_outlined,
      ),
      _FieldOption(
        key: 'Other',
        label: 'Other Details',
        currentValue: 'General contract provisions',
        icon: Icons.more_horiz_rounded,
      ),
    ];
    widget.controller.loadActivePositions();
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
        if (key == 'EmployeePosition') {
          widget.controller.loadActivePositions();
        }
      }
    });
  }

  Future<void> _pickDate(String fieldKey, TextEditingController controller) async {
    DateTime initialDate;
    if (controller.text.isNotEmpty) {
      initialDate = DateTime.tryParse(controller.text) ??
          (fieldKey == 'StartDate'
              ? widget.contract.startDate
              : (widget.contract.endDate ?? widget.contract.startDate.add(const Duration(days: 365))));
    } else {
      initialDate = fieldKey == 'StartDate'
          ? widget.contract.startDate
          : (widget.contract.endDate ?? widget.contract.startDate.add(const Duration(days: 365)));
    }

    final firstDate = DateTime(2020, 1, 1);
    final lastDate = DateTime(2035, 12, 31);

    final picked = await showDatePicker(
      context: context,
      initialDate: initialDate.isBefore(firstDate)
          ? firstDate
          : (initialDate.isAfter(lastDate) ? lastDate : initialDate),
      firstDate: firstDate,
      lastDate: lastDate,
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.primary,
              onPrimary: Colors.white,
              onSurface: AppColors.textPrimary,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      setState(() {
        controller.text =
            '${picked.year.toString().padLeft(4, '0')}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}';
      });
    }
  }

  Future<void> _handleSubmit() async {
    if (!widget.contract.isPendingStaff) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Change requests can only be submitted for contracts pending acceptance (PENDING_STAFF). Active contracts cannot be modified via change request.',
          ),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }
    if (_selectedFieldKeys.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Please select at least one field to request adjustment.',
          ),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }

    if (!_formKey.currentState!.validate()) return;

    // Date validations
    if (_selectedFieldKeys.contains('StartDate') && _selectedFieldKeys.contains('EndDate')) {
      final startStr = _proposedControllers['StartDate']?.text.trim();
      final endStr = _proposedControllers['EndDate']?.text.trim();
      if (startStr != null && endStr != null && startStr.isNotEmpty && endStr.isNotEmpty) {
        final start = DateTime.tryParse(startStr);
        final end = DateTime.tryParse(endStr);
        if (start != null && end != null && !end.isAfter(start)) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Contract End Date must be after Contract Start Date.'),
              backgroundColor: AppColors.error,
            ),
          );
          return;
        }
      }
    } else if (_selectedFieldKeys.contains('StartDate') &&
        !_selectedFieldKeys.contains('EndDate') &&
        widget.contract.endDate != null) {
      final startStr = _proposedControllers['StartDate']?.text.trim();
      if (startStr != null && startStr.isNotEmpty) {
        final start = DateTime.tryParse(startStr);
        if (start != null && !widget.contract.endDate!.isAfter(start)) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Contract Start Date must be before current Contract End Date.'),
              backgroundColor: AppColors.error,
            ),
          );
          return;
        }
      }
    } else if (_selectedFieldKeys.contains('EndDate') && !_selectedFieldKeys.contains('StartDate')) {
      final endStr = _proposedControllers['EndDate']?.text.trim();
      if (endStr != null && endStr.isNotEmpty) {
        final end = DateTime.tryParse(endStr);
        if (end != null && !end.isAfter(widget.contract.startDate)) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Contract End Date must be after current Contract Start Date.'),
              backgroundColor: AppColors.error,
            ),
          );
          return;
        }
      }
    }

    if (_selectedFieldKeys.contains('HourlyRate')) {
      final rateStr = _proposedControllers['HourlyRate']?.text.trim().replaceAll(',', '');
      final rate = double.tryParse(rateStr ?? '');
      if (rate == null || rate <= 0) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Proposed hourly rate must be a valid positive number.'),
            backgroundColor: AppColors.error,
          ),
        );
        return;
      }
      final minWage = widget.contract.regionalMinimumWage;
      if (minWage != null && minWage > 0 && rate < minWage) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Proposed hourly rate cannot be lower than the regional minimum wage (${formatCurrency(minWage)}/hr).',
            ),
            backgroundColor: AppColors.error,
          ),
        );
        return;
      }
    }

    final items = <ContractChangeRequestItemInputModel>[];
    for (var key in _selectedFieldKeys) {
      items.add(
        ContractChangeRequestItemInputModel(
          fieldName: key,
          proposedValue: _proposedControllers[key]!.text.trim(),
          reason: _reasonControllers[key]?.text.trim(),
        ),
      );
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
            content: Text(
              'Contract change request submitted to manager successfully!',
            ),
            backgroundColor: AppColors.success,
          ),
        );
        Navigator.pop(context, true);
      } else {
        _showErrorDialog(
          widget.controller.actionErrorMessage ??
              'Unable to submit change request.',
        );
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
            Text(
              'Request Failed',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Text(
          message,
          style: const TextStyle(fontSize: 14, color: AppColors.textSecondary),
        ),
        actions: [
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10),
              ),
            ),
            child: const Text('Understood'),
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
          'Submit Change Request',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(
            Icons.arrow_back_ios_new_rounded,
            color: AppColors.textPrimary,
            size: 20,
          ),
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
              // Contract Info Banner
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFEFF6FF),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: const Color(0xFFBFDBFE)),
                ),
                child: Row(
                  children: [
                    const Icon(
                      Icons.info_outline_rounded,
                      color: AppColors.primary,
                      size: 22,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Contract: ${widget.contract.contractNumber}',
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: AppColors.primaryDark,
                            ),
                          ),
                          const SizedBox(height: 2),
                          const Text(
                            'Select the fields requiring adjustment to send to your manager for review and revision.',
                            style: TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // 1. Select Fields to Change
              _buildSectionTitle('1. Select Fields to Adjust:'),
              ..._availableFields.map((field) {
                final isChecked = _selectedFieldKeys.contains(field.key);
                return Container(
                  margin: const EdgeInsets.only(bottom: 10),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: isChecked
                          ? AppColors.primary
                          : AppColors.borderLight,
                      width: isChecked ? 1.5 : 1,
                    ),
                  ),
                  child: CheckboxListTile(
                    value: isChecked,
                    activeColor: AppColors.primary,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    title: Text(
                      field.label,
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: isChecked
                            ? FontWeight.bold
                            : FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    subtitle: Text(
                      'Current: ${field.currentValue}',
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textMuted,
                      ),
                    ),
                    secondary: Icon(
                      field.icon,
                      color: isChecked
                          ? AppColors.primary
                          : AppColors.textMuted,
                      size: 22,
                    ),
                    onChanged: (_) => _toggleField(field.key),
                  ),
                );
              }),
              const SizedBox(height: 16),

              // 2. Input Proposed Values for Selected Fields
              if (_selectedFieldKeys.isNotEmpty) ...[
                _buildSectionTitle('2. Enter Proposed Adjustments:'),
                ..._availableFields
                    .where((f) => _selectedFieldKeys.contains(f.key))
                    .map((f) => _buildFieldInputCard(f)),
                const SizedBox(height: 16),
              ],

              // 3. Additional Note for Manager
              _buildSectionTitle('3. Additional Note for Manager:'),
              TextFormField(
                controller: _staffCommentController,
                maxLines: 3,
                maxLength: 300,
                decoration: InputDecoration(
                  hintText:
                      'Enter additional comments or explanation for your manager (optional)...',
                  hintStyle: const TextStyle(
                    fontSize: 13,
                    color: AppColors.textMuted,
                  ),
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
                    borderSide: const BorderSide(
                      color: AppColors.primary,
                      width: 1.5,
                    ),
                  ),
                  contentPadding: const EdgeInsets.all(14),
                ),
              ),
              const SizedBox(height: 24),

              // Submit Button
              AppButton(
                text: 'Submit Change Request',
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
          fontSize: 14,
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
          if (field.key == 'EmployeePosition') ...[
            ListenableBuilder(
              listenable: widget.controller,
              builder: (context, _) {
                final positions = widget.controller.activePositions;
                final isLoading = widget.controller.isLoadingPositions;

                if (isLoading) {
                  return const Padding(
                    padding: EdgeInsets.symmetric(vertical: 12),
                    child: Center(
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          ),
                          SizedBox(width: 8),
                          Text(
                            'Loading active positions...',
                            style: TextStyle(
                              fontSize: 12,
                              color: AppColors.textMuted,
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }

                final currentVal = proposedCtrl.text.trim();
                final dropdownValue = positions.contains(currentVal)
                    ? currentVal
                    : null;

                return DropdownButtonFormField<String>(
                  initialValue: dropdownValue,
                  decoration: InputDecoration(
                    labelText: 'Desired Position *',
                    hintText: '-- Select position from system --',
                    helperText: 'Only active system positions are selectable.',
                    helperStyle: const TextStyle(
                      fontSize: 11,
                      color: AppColors.textMuted,
                    ),
                    labelStyle: const TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 12,
                    ),
                  ),
                  icon: const Icon(
                    Icons.arrow_drop_down_rounded,
                    color: AppColors.primary,
                  ),
                  isExpanded: true,
                  items: positions.map((pos) {
                    return DropdownMenuItem<String>(
                      value: pos,
                      child: Text(
                        pos,
                        style: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    );
                  }).toList(),
                  onChanged: (val) {
                    if (val != null) {
                      setState(() {
                        proposedCtrl.text = val;
                      });
                    }
                  },
                  validator: (val) {
                    if (val == null || val.trim().isEmpty) {
                      return 'Please select a position from the list.';
                    }
                    return null;
                  },
                );
              },
            ),
          ] else if (field.key == 'StartDate' || field.key == 'EndDate') ...[
            TextFormField(
              controller: proposedCtrl,
              readOnly: true,
              onTap: () => _pickDate(field.key, proposedCtrl),
              decoration: InputDecoration(
                labelText: field.key == 'StartDate'
                    ? 'Proposed Start Date *'
                    : 'Proposed End Date *',
                hintText: 'Tap to select date (YYYY-MM-DD)',
                helperText: 'Date will be formatted as YYYY-MM-DD.',
                helperStyle: const TextStyle(
                  fontSize: 11,
                  color: AppColors.textMuted,
                ),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.calendar_month_rounded, color: AppColors.primary),
                  onPressed: () => _pickDate(field.key, proposedCtrl),
                ),
                labelStyle: const TextStyle(
                  fontSize: 13,
                  color: AppColors.textSecondary,
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 12,
                ),
              ),
              validator: (val) {
                if (val == null || val.trim().isEmpty) {
                  return 'Please select a date.';
                }
                return null;
              },
            ),
          ] else if (field.key == 'HourlyRate') ...[
            TextFormField(
              controller: proposedCtrl,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(
                labelText: 'Proposed Hourly Rate (₫/hr) *',
                hintText: 'e.g. 25000',
                helperText: widget.contract.regionalMinimumWage != null && widget.contract.regionalMinimumWage! > 0
                    ? 'Regional minimum wage: ${formatCurrency(widget.contract.regionalMinimumWage!)}/hr'
                    : 'Must be higher than or equal to regional minimum wage.',
                helperStyle: TextStyle(
                  fontSize: 11,
                  color: widget.contract.regionalMinimumWage != null && widget.contract.regionalMinimumWage! > 0
                      ? AppColors.primary
                      : AppColors.textMuted,
                  fontWeight: FontWeight.w600,
                ),
                suffixText: '₫/hr',
                labelStyle: const TextStyle(
                  fontSize: 13,
                  color: AppColors.textSecondary,
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 12,
                ),
              ),
              validator: (val) {
                if (val == null || val.trim().isEmpty) {
                  return 'Please enter a proposed hourly rate.';
                }
                final cleaned = val.trim().replaceAll(',', '');
                final rate = double.tryParse(cleaned);
                if (rate == null) {
                  return 'Hourly rate must be a valid number.';
                }
                if (rate <= 0) {
                  return 'Hourly rate must be greater than 0.';
                }
                final minWage = widget.contract.regionalMinimumWage;
                if (minWage != null && minWage > 0 && rate < minWage) {
                  return 'Rate cannot be lower than regional minimum (${formatCurrency(minWage)}/hr).';
                }
                return null;
              },
            ),
          ] else ...[
            TextFormField(
              controller: proposedCtrl,
              decoration: InputDecoration(
                labelText: 'Proposed New Value *',
                hintText: 'Enter your proposed value',
                labelStyle: const TextStyle(
                  fontSize: 13,
                  color: AppColors.textSecondary,
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 12,
                ),
              ),
              validator: (val) {
                if (val == null || val.trim().isEmpty) {
                  return 'Please enter a proposed value.';
                }
                return null;
              },
            ),
          ],
          const SizedBox(height: 10),
          TextFormField(
            controller: reasonCtrl,
            decoration: InputDecoration(
              labelText: 'Reason for Adjustment',
              hintText: 'Explain why this change is requested (optional)',
              labelStyle: const TextStyle(
                fontSize: 13,
                color: AppColors.textSecondary,
              ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 12,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
