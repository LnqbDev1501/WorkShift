import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/quick_action_item.dart';

import '../../face_registration/face_registration_modal.dart';
import '../../shifts/available_shifts_view.dart';
import '../../shifts/my_shifts_view.dart';
import '../../shifts/shift_swap_list_view.dart';
import '../../requests/staff_request_list_view.dart';
import '../../contracts/staff_contract_list_view.dart';
import '../../orders/staff_order_list_view.dart';
import '../../orders/create_order_view.dart';
import '../../timesheet/staff_timesheet_view.dart';
import '../../payroll/payroll_home_view.dart';
import '../../catalog/product_list_view.dart';
import '../../adjustments/staff_reward_list_view.dart';
import '../../adjustments/staff_penalty_list_view.dart';
import '../../../models/attendance/face_registration_model.dart';
import '../../../services/attendance/face_registration_service.dart';
import '../../../services/orders/staff_order_service.dart';
import '../../../models/shifts/shift_registration_model.dart';

/// Quick Action model definition
class _QuickActionData {
  final String title;
  final String subtitle;
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final VoidCallback onTap;
  final bool isGrayedOut;

  const _QuickActionData({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.iconColor,
    required this.iconBgColor,
    required this.onTap,
    this.isGrayedOut = false,
  });
}

/// Component: Quick Actions on Homepage (Default 6 actions, expandable)
class QuickActions extends StatefulWidget {
  final VoidCallback? onFaceRegistrationSuccess;
  final VoidCallback? onShiftsChanged;
  final VoidCallback? onNavigateToTimesheet;
  final VoidCallback? onNavigateToSchedule;
  final VoidCallback? onNavigateToPayroll;
  final ShiftAccessModel shiftAccess;
  final VoidCallback? onContractsChanged;

  const QuickActions({
    super.key,
    this.onFaceRegistrationSuccess,
    this.onShiftsChanged,
    this.onNavigateToTimesheet,
    this.onNavigateToSchedule,
    this.onNavigateToPayroll,
    this.shiftAccess = ShiftAccessModel.checking,
    this.onContractsChanged,
  });

  @override
  State<QuickActions> createState() => _QuickActionsState();
}

class _QuickActionsState extends State<QuickActions> {
  bool _isExpanded = false;
  final StaffOrderService _staffOrderService = StaffOrderService();
  bool _isInShift = false;

  @override
  void initState() {
    super.initState();
    _checkShiftStatus();
  }

  @override
  void didUpdateWidget(covariant QuickActions oldWidget) {
    super.didUpdateWidget(oldWidget);
    _checkShiftStatus();
  }

  Future<void> _checkShiftStatus() async {
    final inShift = await _staffOrderService.getShiftStatus();
    if (mounted) {
      setState(() {
        _isInShift = inShift;
      });
    }
  }

  bool _canOpenShiftActions() {
    if (widget.shiftAccess.canManageShifts) return true;
    showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Shift actions unavailable'),
        content: Text(
          widget.shiftAccess.blockedReason ??
              ShiftAccessModel.unavailable.blockedReason!,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
    return false;
  }

  Future<void> _handleFaceBiometrics() async {
    final faceService = FaceRegistrationService();

    // Show quick checking dialog
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => const Center(
        child: Card(
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 24, vertical: 20),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircularProgressIndicator(strokeWidth: 2.5),
                SizedBox(width: 16),
                Text(
                  'Checking biometric status...',
                  style: TextStyle(fontSize: 14),
                ),
              ],
            ),
          ),
        ),
      ),
    );

    final FaceProfileStatusModel status;
    try {
      status = await faceService.getStatus();
    } catch (e) {
      if (!mounted) return;
      if (Navigator.canPop(context)) Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Failed to check face status: $e'),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }

    if (!mounted) return;
    if (Navigator.canPop(context)) {
      Navigator.pop(context); // close progress dialog
    }

    // Case 1: Initial Registration (User has not registered yet)
    if (!status.hasRegisteredFace) {
      final success = await showFaceRegistrationModal(context: context);
      if (success == true && mounted) {
        widget.onFaceRegistrationSuccess?.call();
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Face profile registered successfully!'),
            backgroundColor: Color(0xFF10B981),
          ),
        );
      }
      return;
    }

    // Case 2: Already registered, but already updated this calendar month
    if (!status.canRegisterOrUpdate) {
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          title: const Row(
            children: [
              Icon(Icons.calendar_month_rounded, color: Color(0xFFF59E0B)),
              SizedBox(width: 8),
              Text(
                'Monthly Limit Reached',
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
              ),
            ],
          ),
          content: Text(
            status.message ??
                'You have already updated your biometric face profile this month. Re-registration is only allowed once per calendar month.',
            style: const TextStyle(
              fontSize: 14,
              color: AppColors.textSecondary,
              height: 1.4,
            ),
          ),
          actions: [
            ElevatedButton(
              onPressed: () => Navigator.pop(ctx),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: const Text('Got It'),
            ),
          ],
        ),
      );
      return;
    }

    // Case 3: Already registered, eligible for monthly re-registration
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.face_unlock_rounded, color: Color(0xFF7C3AED)),
            SizedBox(width: 8),
            Text(
              'Update Face Profile',
              style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: const Text(
          'You have already registered your face profile. You are eligible to update it once this month. The new face scan will replace your existing biometric profile for shift attendance.\n\nDo you want to re-register now?',
          style: TextStyle(
            fontSize: 14,
            color: AppColors.textSecondary,
            height: 1.4,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text(
              'Cancel',
              style: TextStyle(color: AppColors.textSecondary),
            ),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFF7C3AED),
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
            ),
            child: const Text('Proceed'),
          ),
        ],
      ),
    );

    if (!mounted || confirmed != true) return;
    final success = await showFaceRegistrationModal(context: context);
    if (success == true && mounted) {
      widget.onFaceRegistrationSuccess?.call();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Biometric face profile updated successfully for this month!',
          ),
          backgroundColor: Color(0xFF10B981),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    // Complete quick action item list
    final List<_QuickActionData> allActions = [
      _QuickActionData(
        title: 'Face Biometrics',
        subtitle: 'Setup or update',
        icon: Icons.face_retouching_natural_rounded,
        iconColor: const Color(0xFF7C3AED),
        iconBgColor: const Color(0xFFF3E8FF),
        onTap: _handleFaceBiometrics,
      ),
      _QuickActionData(
        title: 'Submit Request',
        subtitle: 'Leave, late, salary',
        icon: Icons.edit_note_rounded,
        iconColor: AppColors.actionLeaveIcon,
        iconBgColor: AppColors.actionLeaveBg,
        onTap: () {
          Navigator.of(context).push(
            MaterialPageRoute(
              builder: (_) => const StaffRequestListView(showBackButton: true),
            ),
          );
        },
      ),
      _QuickActionData(
        title: 'Available Shifts',
        subtitle: 'Browse open shifts',
        icon: Icons.event_note_rounded,
        iconColor: AppColors.actionRegisterIcon,
        iconBgColor: AppColors.actionRegisterBg,
        isGrayedOut: !widget.shiftAccess.canManageShifts,
        onTap: () async {
          if (!_canOpenShiftActions()) return;
          await Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const AvailableShiftsView()),
          );
          if (mounted) widget.onShiftsChanged?.call();
        },
      ),
      _QuickActionData(
        title: 'Shift Swap',
        subtitle: 'Find colleague',
        icon: Icons.swap_horiz_rounded,
        iconColor: AppColors.actionSwapIcon,
        iconBgColor: AppColors.actionSwapBg,
        isGrayedOut: !widget.shiftAccess.canManageShifts,
        onTap: () async {
          if (!_canOpenShiftActions()) return;
          await Navigator.of(
            context,
          ).push(MaterialPageRoute(builder: (_) => const ShiftSwapListView()));
          if (mounted) widget.onShiftsChanged?.call();
        },
      ),
      _QuickActionData(
        title: 'Contracts',
        subtitle: 'E-documents & rate',
        icon: Icons.verified_user_rounded,
        iconColor: AppColors.actionContractIcon,
        iconBgColor: AppColors.actionContractBg,
        onTap: () async {
          await Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const StaffContractListView()),
          );
          if (mounted) widget.onContractsChanged?.call();
        },
      ),
      _QuickActionData(
        title: 'Create Order',
        subtitle: 'POS counter',
        icon: Icons.point_of_sale_rounded,
        iconColor: AppColors.actionOrderIcon,
        iconBgColor: AppColors.actionOrderBg,
        isGrayedOut: !_isInShift,
        onTap: () async {
          final inShift = await _staffOrderService.getShiftStatus();
          if (!mounted) return;
          setState(() => _isInShift = inShift);
          if (!context.mounted) return;
          if (!inShift) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text(
                  'You must be in your shift and checked in to perform this action.',
                ),
                backgroundColor: AppColors.error,
              ),
            );
            return;
          }
          await Navigator.of(
            context,
          ).push(MaterialPageRoute(builder: (_) => const CreateOrderView()));
          if (mounted) {
            _checkShiftStatus();
          }
        },
      ),
      // --- Extended actions when expanded ---
      _QuickActionData(
        title: 'Order History',
        subtitle: 'View receipts',
        icon: Icons.receipt_long_rounded,
        iconColor: AppColors.actionHistoryIcon,
        iconBgColor: AppColors.actionHistoryBg,
        onTap: () {
          Navigator.of(
            context,
          ).push(MaterialPageRoute(builder: (_) => const StaffOrderListView()));
        },
      ),
      _QuickActionData(
        title: 'Timesheet',
        subtitle: 'Weekly hours',
        icon: Icons.calendar_month_outlined,
        iconColor: AppColors.actionOrderIcon,
        iconBgColor: AppColors.actionOrderBg,
        onTap: () {
          if (widget.onNavigateToTimesheet != null) {
            widget.onNavigateToTimesheet!();
          } else {
            Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const StaffTimesheetView()),
            );
          }
        },
      ),
      _QuickActionData(
        title: 'Work Schedule',
        subtitle: 'Your shifts and details',
        icon: Icons.event_note_outlined,
        iconColor: AppColors.actionRegisterIcon,
        iconBgColor: AppColors.actionRegisterBg,
        onTap: () async {
          if (widget.onNavigateToSchedule != null) {
            widget.onNavigateToSchedule!();
          } else {
            await Navigator.of(
              context,
            ).push(MaterialPageRoute(builder: (_) => const MyShiftsView()));
            if (mounted) widget.onShiftsChanged?.call();
          }
        },
      ),
      _QuickActionData(
        title: 'Payslip',
        subtitle: 'View income',
        icon: Icons.payments_rounded,
        iconColor: AppColors.actionSalaryIcon,
        iconBgColor: AppColors.actionSalaryBg,
        onTap: () {
          if (widget.onNavigateToPayroll != null) {
            widget.onNavigateToPayroll!();
          } else {
            Navigator.of(
              context,
            ).push(MaterialPageRoute(builder: (_) => const PayrollHomeView()));
          }
        },
      ),
      _QuickActionData(
        title: 'Products',
        subtitle: 'Branch menu & catalog',
        icon: Icons.fastfood_rounded,
        iconColor: AppColors.primary,
        iconBgColor: AppColors.primaryLight,
        onTap: () {
          Navigator.of(
            context,
          ).push(MaterialPageRoute(builder: (_) => const ProductListView()));
        },
      ),
      _QuickActionData(
        title: 'My Rewards',
        subtitle: 'Bonuses & praise',
        icon: Icons.workspace_premium_rounded,
        iconColor: AppColors.success,
        iconBgColor: AppColors.successBg,
        onTap: () {
          Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const StaffRewardListView()),
          );
        },
      ),
      _QuickActionData(
        title: 'My Penalties',
        subtitle: 'Infractions & deduction',
        icon: Icons.gavel_rounded,
        iconColor: AppColors.error,
        iconBgColor: AppColors.errorBg,
        onTap: () {
          Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const StaffPenaltyListView()),
          );
        },
      ),
    ];

    // Limit to 6 items when collapsed
    final displayedActions = _isExpanded
        ? allActions
        : allActions.take(6).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Quick Actions',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 12),

        const Text(
          'Everything you need for your workday',
          style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
        ),
        const SizedBox(height: 16),
        // Display actions in 2-column grid rows
        for (int i = 0; i < displayedActions.length; i += 2) ...[
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: QuickActionItem(
                  title: displayedActions[i].title,
                  subtitle: displayedActions[i].subtitle,
                  icon: displayedActions[i].icon,
                  iconColor: displayedActions[i].iconColor,
                  iconBgColor: displayedActions[i].iconBgColor,
                  isGrayedOut: displayedActions[i].isGrayedOut,
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
                    isGrayedOut: displayedActions[i + 1].isGrayedOut,
                    onTap: displayedActions[i + 1].onTap,
                  ),
                )
              else
                const Expanded(child: SizedBox()),
            ],
          ),
          if (i + 2 < displayedActions.length) const SizedBox(height: 12),
        ],
        const SizedBox(height: 12),
        Center(
          child: TextButton.icon(
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
              _isExpanded
                  ? Icons.keyboard_arrow_up_rounded
                  : Icons.keyboard_arrow_down_rounded,
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
        ),
      ],
    );
  }
}
