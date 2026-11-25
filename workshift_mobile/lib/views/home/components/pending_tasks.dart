import 'package:flutter/material.dart';
import '../../../models/requests/staff_request_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/status_badge.dart';

/// The three requests most recently changed, supplied by HomeController.
class PendingTasks extends StatelessWidget {
  final List<StaffEmployeeRequestListItemModel> requests;
  final String? errorMessage;
  final ValueChanged<StaffEmployeeRequestListItemModel> onRequestTap;
  final VoidCallback onViewAll;

  const PendingTasks({
    super.key,
    required this.requests,
    required this.onRequestTap,
    required this.onViewAll,
    this.errorMessage,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Recent Requests',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 6),
        const Text(
          'Latest updates on your requests',
          style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
        ),
        const SizedBox(height: 16),
        if (errorMessage != null)
          Text(errorMessage!, style: const TextStyle(color: AppColors.error))
        else if (requests.isEmpty)
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.border),
            ),
            child: const Column(
              children: [
                Icon(Icons.inbox_outlined, color: AppColors.primary, size: 32),
                SizedBox(height: 10),
                Text(
                  'No requests yet',
                  style: TextStyle(fontWeight: FontWeight.w700),
                ),
                SizedBox(height: 4),
                Text(
                  'Your request updates will appear here.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: AppColors.textSecondary),
                ),
              ],
            ),
          )
        else
          for (final request in requests) ...[
            _RequestTile(request: request, onTap: () => onRequestTap(request)),
            const SizedBox(height: 10),
          ],
        Center(
          child: TextButton.icon(
            onPressed: onViewAll,
            icon: const Icon(Icons.arrow_forward_rounded, size: 17),
            label: const Text('View all requests'),
          ),
        ),
      ],
    );
  }
}

class _RequestTile extends StatelessWidget {
  final StaffEmployeeRequestListItemModel request;
  final VoidCallback onTap;

  const _RequestTile({required this.request, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final statusColor = request.isApproved
        ? const Color(0xFF047857)
        : request.isRejected
        ? AppColors.error
        : request.isPending
        ? AppColors.warning
        : AppColors.textSecondary;
    final updated =
        (request.updatedAt ?? request.reviewedAt ?? request.createdAt)
            .toLocal();
    final localizations = MaterialLocalizations.of(context);
    final timestamp =
        '${localizations.formatShortDate(updated)} · '
        '${localizations.formatTimeOfDay(TimeOfDay.fromDateTime(updated))}';

    return Material(
      color: AppColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: AppColors.border),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(11),
                decoration: BoxDecoration(
                  color: request.typeColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(
                  request.typeIcon,
                  color: request.typeColor,
                  size: 23,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '#${request.requestId} · ${request.typeDisplayName}',
                      style: const TextStyle(
                        fontWeight: FontWeight.w700,
                        fontSize: 14,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      timestamp,
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 10),
                    StatusBadge(
                      text: request.statusDisplayName,
                      backgroundColor: statusColor.withValues(alpha: 0.1),
                      textColor: statusColor,
                    ),
                  ],
                ),
              ),
              const Icon(
                Icons.chevron_right_rounded,
                color: AppColors.textMuted,
                size: 20,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
