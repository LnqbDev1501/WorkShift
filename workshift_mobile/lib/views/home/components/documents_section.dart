import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';

/// Weekly recorded work duration; calculations belong to HomeController.
class DocumentsSection extends StatelessWidget {
  final int workingMinutes;
  final DateTime weekStart;
  final DateTime weekEnd;
  final String? errorMessage;
  final VoidCallback onViewHistory;

  const DocumentsSection({
    super.key,
    required this.workingMinutes,
    required this.weekStart,
    required this.weekEnd,
    required this.onViewHistory,
    this.errorMessage,
  });

  @override
  Widget build(BuildContext context) {
    final localizations = MaterialLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'This Week',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 14),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            color: AppColors.homeHeroStart,
            borderRadius: BorderRadius.circular(24),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(Icons.timelapse_rounded, color: AppColors.homeAccent),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Recorded working hours',
                      style: TextStyle(color: Colors.white70, fontSize: 14),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 20),
              if (errorMessage != null)
                Text(
                  errorMessage!,
                  style: const TextStyle(color: Colors.white, height: 1.5),
                )
              else ...[
                Text(
                  '${workingMinutes ~/ 60}h ${workingMinutes % 60}m',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 38,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -1,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Based on attendance recorded by the system',
                  style: TextStyle(color: Colors.white70, fontSize: 12),
                ),
              ],
              const SizedBox(height: 16),
              Text(
                '${localizations.formatShortDate(weekStart)} – '
                '${localizations.formatShortDate(weekEnd)}',
                style: const TextStyle(
                  color: AppColors.homeAccent,
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 12),
              TextButton.icon(
                onPressed: onViewHistory,
                style: TextButton.styleFrom(
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.zero,
                ),
                icon: const Icon(Icons.arrow_forward_rounded, size: 17),
                label: const Text('View attendance history'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
