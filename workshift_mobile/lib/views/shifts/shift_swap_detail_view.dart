import 'package:flutter/material.dart';
import '../../controllers/shifts/shift_swap_detail_controller.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_info_row.dart';
import '../../widgets/app_note_confirmation_dialog.dart';
import '../../widgets/app_state_panel.dart';
import 'components/shift_display_format.dart';
import 'components/shift_swap_summary.dart';

class ShiftSwapDetailView extends StatefulWidget {
  final int requestId;
  final ShiftSwapDetailController? controller;
  const ShiftSwapDetailView({
    super.key,
    required this.requestId,
    this.controller,
  });
  @override
  State<ShiftSwapDetailView> createState() => _ShiftSwapDetailViewState();
}

class _ShiftSwapDetailViewState extends State<ShiftSwapDetailView>
    with WidgetsBindingObserver {
  late final _controller =
      widget.controller ??
      ShiftSwapDetailController(requestId: widget.requestId);
  bool _confirming = false;
  final Map<ShiftSwapAction, String> _notes = {};
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _controller.refresh();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && !_confirming) {
      _controller.refresh();
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    if (widget.controller == null) _controller.dispose();
    super.dispose();
  }

  Future<void> _respond(ShiftSwapAction action) async {
    if (_confirming || _controller.isSaving) return;
    final request = _controller.request;
    if (request == null) return;
    _confirming = true;
    try {
      final accept = action == ShiftSwapAction.accept;
      final cancel = action == ShiftSwapAction.cancel;
      final label = accept
          ? 'Accept swap'
          : cancel
          ? 'Cancel request'
          : 'Reject request';
      final give = _controller.isSent
          ? request.sourceAssignment
          : request.targetAssignment;
      final receive = _controller.isSent
          ? request.targetAssignment
          : request.sourceAssignment;
      final note = await showAppNoteConfirmation(
        context,
        title: '$label?',
        confirmLabel: label,
        isDestructive: !accept,
        showNote: !accept,
        initialNote: _notes[action] ?? '',
        message: accept
            ? 'You give ${give.shiftName}:\n${swapAssignmentText(give)}\n\nYou receive ${receive.shiftName}:\n${swapAssignmentText(receive)}\n\nBoth work schedules will change immediately. Times are UTC+7.'
            : cancel
            ? 'Withdraw this swap request? Your assigned shifts will stay the same.'
            : 'Decline this swap request? Your assigned shifts will stay the same.',
      );
      if (!mounted || note == null) return;
      _notes[action] = note;
      final success = await _controller.respond(action, note: note);
      if (!mounted) return;
      if (success) {
        _notes.remove(action);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              accept
                  ? 'Swap accepted. Both work schedules have been updated.'
                  : cancel
                  ? 'Swap request cancelled.'
                  : 'Swap request rejected.',
            ),
          ),
        );
      }
    } finally {
      _confirming = false;
    }
  }

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: _controller,
    builder: (context, _) {
      final request = _controller.request;
      return PopScope(
        canPop: !_controller.isSaving,
        child: Scaffold(
          appBar: AppBar(
            title: const Text('Swap Details'),
            actions: [
              IconButton(
                tooltip: 'Refresh request',
                onPressed: _controller.isSaving || _controller.isLoading
                    ? null
                    : _controller.refresh,
                icon: const Icon(Icons.refresh_rounded),
              ),
            ],
          ),
          bottomNavigationBar: request == null
              ? null
              : SafeArea(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                    child: _controller.isSaving
                        ? const SizedBox(
                            height: 50,
                            child: Center(child: CircularProgressIndicator()),
                          )
                        : _controller.canCancel
                        ? AppButton(
                            text: 'Cancel request',
                            type: AppButtonType.dangerOutline,
                            onPressed: () => _respond(ShiftSwapAction.cancel),
                          )
                        : _controller.canRespond
                        ? Wrap(
                            spacing: 10,
                            runSpacing: 8,
                            children: [
                              AppButton(
                                text: 'Reject request',
                                type: AppButtonType.dangerOutline,
                                onPressed: () =>
                                    _respond(ShiftSwapAction.reject),
                              ),
                              AppButton(
                                text: 'Accept swap',
                                onPressed: () =>
                                    _respond(ShiftSwapAction.accept),
                              ),
                            ],
                          )
                        : const SizedBox.shrink(),
                  ),
                ),
          body: SafeArea(
            child: RefreshIndicator(
              onRefresh: _controller.refresh,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16),
                children: [
                  if (_controller.isLoading)
                    const Padding(
                      padding: EdgeInsets.all(48),
                      child: Center(child: CircularProgressIndicator()),
                    )
                  else if (request == null)
                    AppStatePanel(
                      icon: Icons.cloud_off_outlined,
                      title: 'Request unavailable',
                      message: _controller.error ?? 'Please try again.',
                      actionLabel: 'Try again',
                      onAction: _controller.refresh,
                      isError: true,
                    )
                  else ...[
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Wrap(
                              spacing: 12,
                              runSpacing: 8,
                              crossAxisAlignment: WrapCrossAlignment.center,
                              children: [
                                Text(
                                  'Request #${request.swapRequestId}',
                                  style: const TextStyle(
                                    fontSize: 18,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                ShiftSwapStatusBadge(
                                  status: _controller.effectiveStatus!,
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            Text(
                              _statusMessage(_controller.effectiveStatus!),
                              style: const TextStyle(
                                fontSize: 13,
                                height: 1.5,
                                color: AppColors.textSecondary,
                              ),
                            ),
                            const SizedBox(height: 16),
                            ShiftSwapSummary(
                              give: _controller.isSent
                                  ? request.sourceAssignment
                                  : request.targetAssignment,
                              receive: _controller.isSent
                                  ? request.targetAssignment
                                  : request.sourceAssignment,
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              'Vietnam time · UTC+7',
                              style: TextStyle(
                                fontSize: 11,
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          children: [
                            AppInfoRow(
                              icon: Icons.person_outline,
                              label: 'Requested by',
                              value: request.requesterName,
                            ),
                            AppInfoRow(
                              icon: Icons.person_outline,
                              label: 'Sent to',
                              value: request.targetUserName,
                            ),
                            AppInfoRow(
                              icon: Icons.schedule_rounded,
                              label: 'Created',
                              value: ShiftDisplayFormat.deadline(
                                request.createdAt,
                              ),
                            ),
                            AppInfoRow(
                              icon: Icons.notes_rounded,
                              label: 'Reason',
                              value: request.reason ?? 'No reason provided.',
                            ),
                            AppInfoRow(
                              icon: Icons.timer_outlined,
                              label: 'Expires at',
                              value: ShiftDisplayFormat.deadline(
                                request.expiresAt,
                              ),
                            ),
                            if (request.respondedAt != null)
                              AppInfoRow(
                                icon: Icons.update_rounded,
                                label: 'Responded',
                                value: ShiftDisplayFormat.deadline(
                                  request.respondedAt!,
                                ),
                              ),
                            if (request.responseNote?.isNotEmpty == true)
                              AppInfoRow(
                                icon: Icons.comment_outlined,
                                label: 'Response note',
                                value: request.responseNote!,
                              ),
                          ],
                        ),
                      ),
                    ),
                    if (_controller.actionError != null) ...[
                      const SizedBox(height: 12),
                      Text(
                        _controller.actionError!,
                        style: const TextStyle(color: AppColors.error),
                      ),
                      TextButton(
                        onPressed: _controller.isSaving
                            ? null
                            : _controller.refresh,
                        child: const Text('Refresh request'),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          ),
        ),
      );
    },
  );

  String _statusMessage(String status) => switch (status) {
    'PENDING' =>
      _controller.isSent
          ? 'Waiting for your colleague to respond. Keep working your current schedule until this swap is accepted.'
          : 'Review both shifts before responding. Accepting will exchange your assigned shifts immediately.',
    'ACCEPTED' =>
      'This swap was accepted. Your work schedule has been updated.',
    'REJECTED' =>
      'This request was rejected. The original assignments were kept.',
    'CANCELLED' =>
      'This request was cancelled. The original assignments were kept.',
    'EXPIRED' =>
      'This request expired when the earlier shift started. The original assignments were kept.',
    _ => 'This request cannot be changed.',
  };
}
