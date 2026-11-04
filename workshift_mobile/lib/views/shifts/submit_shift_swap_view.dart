import 'package:flutter/material.dart';
import '../../controllers/shifts/submit_shift_swap_controller.dart';
import '../../models/shifts/available_shift_model.dart';
import '../../models/shifts/shift_swap_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_confirmation_dialog.dart';
import '../../widgets/app_selection_sheet.dart';
import '../../widgets/app_state_panel.dart';
import 'components/shift_swap_summary.dart';

class SubmitShiftSwapView extends StatefulWidget {
  final SubmitShiftSwapController? controller;
  const SubmitShiftSwapView({super.key, this.controller});
  @override
  State<SubmitShiftSwapView> createState() => _SubmitShiftSwapViewState();
}

class _SubmitShiftSwapViewState extends State<SubmitShiftSwapView> {
  late final _controller = widget.controller ?? SubmitShiftSwapController();
  final _reason = TextEditingController();
  final _form = GlobalKey<FormState>();
  bool _confirming = false;
  @override
  void initState() {
    super.initState();
    _controller.loadSources();
  }

  @override
  void dispose() {
    _reason.dispose();
    if (widget.controller == null) _controller.dispose();
    super.dispose();
  }

  ShiftSwapAssignmentModel _assignment(AvailableShiftModel shift) =>
      ShiftSwapAssignmentModel(
        assignmentId: shift.myRegistration!.assignmentId,
        shiftId: shift.shiftId,
        branchId: shift.branchId,
        shiftName: shift.shiftName,
        scheduledStartAt: shift.scheduledStartAt,
        scheduledEndAt: shift.scheduledEndAt,
      );

  Future<void> _chooseSource() async {
    final value = await showAppSelectionSheet<AvailableShiftModel>(
      context,
      title: 'Choose your shift',
      options: _controller.sources
          .map(
            (shift) => AppSelectionOption(
              value: shift,
              title: shift.shiftName,
              subtitle:
                  '${swapAssignmentText(_assignment(shift))}\n${shift.branchName}',
            ),
          )
          .toList(),
    );
    if (mounted && value != null) await _controller.selectSource(value);
  }

  Future<void> _chooseTarget() async {
    final value = await showAppSelectionSheet<ShiftSwapCandidateModel>(
      context,
      title: 'Choose colleague’s shift',
      options: _controller.candidates
          .map(
            (candidate) => AppSelectionOption(
              value: candidate,
              title: candidate.staffName,
              subtitle:
                  '${candidate.assignment.shiftName}\n${swapAssignmentText(candidate.assignment)}',
            ),
          )
          .toList(),
    );
    if (mounted && value != null) _controller.selectTarget(value);
  }

  Future<void> _submit() async {
    if (_confirming ||
        !_controller.canSubmit ||
        !_form.currentState!.validate()) {
      return;
    }
    _confirming = true;
    try {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (_) => AppConfirmationDialog(
          title: 'Send swap request?',
          confirmLabel: 'Send request',
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Send to ${_controller.target!.staffName}',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 12),
              ShiftSwapSummary(
                give: _assignment(_controller.source!),
                receive: _controller.target!.assignment,
              ),
              const SizedBox(height: 12),
              const Text(
                'Your schedule changes only when your colleague accepts. Times are UTC+7.',
              ),
            ],
          ),
        ),
      );
      if (!mounted || confirmed != true) return;
      final result = await _controller.submit(_reason.text);
      if (mounted && result != null) Navigator.pop(context, result);
    } finally {
      _confirming = false;
    }
  }

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: _controller,
    builder: (context, _) => PopScope(
      canPop: !_controller.isSaving,
      child: Scaffold(
        appBar: AppBar(title: const Text('New Swap Request')),
        bottomNavigationBar: SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            child: AppButton(
              text: 'Review & send request',
              isLoading: _controller.isSaving,
              onPressed: _controller.canSubmit ? _submit : null,
            ),
          ),
        ),
        body: SafeArea(
          child: Form(
            key: _form,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                const Text(
                  'Choose the shift you want to give, then the shift you want to receive. Both shifts must start at least 24 hours from now.',
                  style: TextStyle(
                    fontSize: 13,
                    height: 1.5,
                    color: AppColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 16),
                if (_controller.isLoading)
                  const Center(child: CircularProgressIndicator())
                else if (_controller.error != null)
                  AppStatePanel(
                    icon: Icons.cloud_off_outlined,
                    title: 'Couldn’t load your shifts',
                    message: _controller.error!,
                    actionLabel: 'Try again',
                    onAction: _controller.loadSources,
                    isError: true,
                  )
                else if (_controller.sources.isEmpty)
                  AppStatePanel(
                    icon: Icons.event_busy_outlined,
                    title: 'No eligible shifts',
                    message:
                        'Assigned shifts must start at least 24 hours from now and meet the swap requirements.',
                    actionLabel: 'Refresh',
                    onAction: _controller.loadSources,
                  )
                else ...[
                  _picker(
                    '1. Your shift to give',
                    _controller.source == null
                        ? 'Choose your shift'
                        : '${_controller.source!.shiftName}\n${swapAssignmentText(_assignment(_controller.source!))}',
                    _controller.isSaving ? null : _chooseSource,
                  ),
                  const SizedBox(height: 12),
                  _picker(
                    '2. Colleague’s shift to receive',
                    _controller.target == null
                        ? 'Choose colleague’s shift'
                        : '${_controller.target!.staffName} · ${_controller.target!.assignment.shiftName}\n${swapAssignmentText(_controller.target!.assignment)}',
                    _controller.isSaving ||
                            _controller.isLoadingCandidates ||
                            _controller.candidates.isEmpty
                        ? null
                        : _chooseTarget,
                  ),
                  if (_controller.source == null)
                    const Padding(
                      padding: EdgeInsets.only(top: 8),
                      child: Text(
                        'Choose your shift first.',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ),
                  if (_controller.isLoadingCandidates)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 12),
                      child: LinearProgressIndicator(),
                    ),
                  if (_controller.candidateError != null) ...[
                    const SizedBox(height: 8),
                    Text(
                      _controller.candidateError!,
                      style: const TextStyle(color: AppColors.error),
                    ),
                    TextButton(
                      onPressed: _controller.isSaving
                          ? null
                          : () => _controller.selectSource(_controller.source!),
                      child: const Text('Find matching shifts again'),
                    ),
                  ] else if (_controller.source != null &&
                      !_controller.isLoadingCandidates &&
                      _controller.candidates.isEmpty)
                    const Padding(
                      padding: EdgeInsets.only(top: 8),
                      child: Text(
                        'No matching shifts for this shift. Choose another shift or try again later.',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _reason,
                    enabled: !_controller.isSaving,
                    minLines: 2,
                    maxLines: 4,
                    maxLength: 500,
                    decoration: const InputDecoration(
                      labelText: 'Reason (optional)',
                      hintText: 'Help your colleague understand your request.',
                    ),
                    validator: (value) => (value?.length ?? 0) > 500
                        ? 'Maximum 500 characters.'
                        : null,
                  ),
                  const Text(
                    'Only matching shifts from your branch are shown. Both schedules are checked again when the request is sent and accepted.',
                    style: TextStyle(
                      fontSize: 12,
                      height: 1.5,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Vietnam time · UTC+7',
                    style: TextStyle(
                      fontSize: 11,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  if (_controller.submitError != null) ...[
                    const SizedBox(height: 12),
                    Text(
                      _controller.submitError!,
                      style: const TextStyle(color: AppColors.error),
                    ),
                    TextButton(
                      onPressed: _controller.isSaving
                          ? null
                          : () => _controller.selectSource(_controller.source!),
                      child: const Text('Refresh matching shifts'),
                    ),
                  ],
                ],
              ],
            ),
          ),
        ),
      ),
    ),
  );

  Widget _picker(String label, String value, VoidCallback? onTap) => Card(
    child: ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      title: Text(
        label,
        style: const TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w600,
          color: AppColors.textSecondary,
        ),
      ),
      subtitle: Padding(
        padding: const EdgeInsets.only(top: 6),
        child: Text(
          value,
          style: TextStyle(
            fontSize: 14,
            height: 1.5,
            color: onTap == null
                ? AppColors.textSecondary
                : AppColors.textPrimary,
          ),
        ),
      ),
      trailing: const Icon(Icons.expand_more_rounded),
      onTap: onTap,
    ),
  );
}
