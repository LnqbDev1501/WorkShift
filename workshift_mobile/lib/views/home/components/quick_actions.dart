import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/quick_action_item.dart';

import '../../face_registration/face_registration_modal.dart';

/// Quick Action model definition
class _QuickActionData {
  final String title;
  final String subtitle;
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final VoidCallback onTap;

  const _QuickActionData({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.iconColor,
    required this.iconBgColor,
    required this.onTap,
  });
}

/// Component: Quick Actions on Homepage (Default 6 actions, expandable)
class QuickActions extends StatefulWidget {
  final VoidCallback? onFaceRegistrationSuccess;

  const QuickActions({super.key, this.onFaceRegistrationSuccess});

  @override
  State<QuickActions> createState() => _QuickActionsState();
}

class _QuickActionsState extends State<QuickActions> {
  bool _isExpanded = false;

  @override
  Widget build(BuildContext context) {
    // Complete quick action item list
    final List<_QuickActionData> allActions = [
      _QuickActionData(
        title: 'Register Face',
        subtitle: 'Biometric setup',
        icon: Icons.face_retouching_natural_rounded,
        iconColor: const Color(0xFF7C3AED),
        iconBgColor: const Color(0xFFF3E8FF),
        onTap: () async {
          final registered = await showFaceRegistrationModal(context: context);
          if (registered == true) {
            widget.onFaceRegistrationSuccess?.call();
          }
        },
      ),
      _QuickActionData(
        title: 'Timesheet',
        subtitle: 'Weekly hours',
        icon: Icons.calendar_month_outlined,
        iconColor: AppColors.actionOrderIcon,
        iconBgColor: AppColors.actionOrderBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to Timesheet')),
          );
        },
      ),
      _QuickActionData(
        title: 'Shift Schedule',
        subtitle: 'Next week',
        icon: Icons.event_note_rounded,
        iconColor: AppColors.actionRegisterIcon,
        iconBgColor: AppColors.actionRegisterBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to Shift Registration')),
          );
        },
      ),
      _QuickActionData(
        title: 'Create Order',
        subtitle: 'POS counter',
        icon: Icons.point_of_sale_rounded,
        iconColor: AppColors.actionOrderIcon,
        iconBgColor: AppColors.actionOrderBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to POS Order Creation')),
          );
        },
      ),
      _QuickActionData(
        title: 'Order History',
        subtitle: 'View receipts',
        icon: Icons.receipt_long_rounded,
        iconColor: AppColors.actionHistoryIcon,
        iconBgColor: AppColors.actionHistoryBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to Order History')),
          );
        },
      ),
      _QuickActionData(
        title: 'Shift Swap',
        subtitle: 'Find colleague',
        icon: Icons.swap_horiz_rounded,
        iconColor: AppColors.actionSwapIcon,
        iconBgColor: AppColors.actionSwapBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to Shift Swap')),
          );
        },
      ),
      _QuickActionData(
        title: 'Submit Request',
        subtitle: 'Leave, overtime',
        icon: Icons.edit_note_rounded,
        iconColor: AppColors.actionLeaveIcon,
        iconBgColor: AppColors.actionLeaveBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to Leave Requests')),
          );
        },
      ),
      // --- Extended actions when expanded ---
      _QuickActionData(
        title: 'Payslip',
        subtitle: 'View income',
        icon: Icons.payments_rounded,
        iconColor: AppColors.actionSalaryIcon,
        iconBgColor: AppColors.actionSalaryBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to Payslip')),
          );
        },
      ),
      _QuickActionData(
        title: 'Contract',
        subtitle: 'E-documents',
        icon: Icons.verified_user_rounded,
        iconColor: AppColors.actionContractIcon,
        iconBgColor: AppColors.actionContractBg,
        onTap: () {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Navigating to Contracts')),
          );
        },
      ),
    ];

    // Limit to 6 items when collapsed
    final displayedActions = _isExpanded ? allActions : allActions.take(6).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Title + See more / Collapse button
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'Quick Actions',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            if (allActions.length > 6)
              TextButton.icon(
                onPressed: () {
                  setState(() {
                    _isExpanded = !_isExpanded;
                  });
                },
                style: TextButton.styleFrom(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  visualDensity: VisualDensity.compact,
                ),
                icon: Icon(
                  _isExpanded ? Icons.keyboard_arrow_up_rounded : Icons.keyboard_arrow_down_rounded,
                  size: 18,
                  color: AppColors.primary,
                ),
                label: Text(
                  _isExpanded ? 'Collapse' : 'See more',
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: AppColors.primary,
                  ),
                ),
              ),
          ],
        ),
        const SizedBox(height: 12),

        // Display actions in 2-column grid rows
        for (int i = 0; i < displayedActions.length; i += 2) ...[
          Row(
            children: [
              Expanded(
                child: QuickActionItem(
                  title: displayedActions[i].title,
                  subtitle: displayedActions[i].subtitle,
                  icon: displayedActions[i].icon,
                  iconColor: displayedActions[i].iconColor,
                  iconBgColor: displayedActions[i].iconBgColor,
                  onTap: displayedActions[i].onTap,
                ),
              ),
              const SizedBox(width: 12),
              if (i + 1 < displayedActions.length)
                Expanded(
                  child: QuickActionItem(
                    title: displayedActions[i + 1].title,
                    subtitle: displayedActions[i + 1].subtitle,
                    icon: displayedActions[i + 1].icon,
                    iconColor: displayedActions[i + 1].iconColor,
                    iconBgColor: displayedActions[i + 1].iconBgColor,
                    onTap: displayedActions[i + 1].onTap,
                  ),
                )
              else
                const Expanded(child: SizedBox()),
            ],
          ),
          if (i + 2 < displayedActions.length) const SizedBox(height: 12),
        ],

        // Bottom toggle button
        if (allActions.length > 6) ...[
          const SizedBox(height: 8),
          Center(
            child: InkWell(
              onTap: () {
                setState(() {
                  _isExpanded = !_isExpanded;
                });
              },
              borderRadius: BorderRadius.circular(20),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      _isExpanded
                          ? 'Collapse list'
                          : 'See ${allActions.length - 6} more actions',
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(width: 4),
                    Icon(
                      _isExpanded ? Icons.keyboard_arrow_up_rounded : Icons.keyboard_arrow_down_rounded,
                      size: 16,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ],
    );
  }
}
