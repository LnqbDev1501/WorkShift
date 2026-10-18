import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';

/// Staff identity and daily welcome on the home screen.
class HomeHeader extends StatelessWidget {
  final String userName;
  final String branchName;
  final String? avatarUrl;
  final VoidCallback? onLogoutTap;
  final VoidCallback? onNotificationTap;
  final int unreadCount;

  const HomeHeader({
    super.key,
    required this.userName,
    required this.branchName,
    this.avatarUrl,
    this.onLogoutTap,
    this.onNotificationTap,
    this.unreadCount = 0,
  });

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final greeting = now.hour < 12
        ? 'Good morning'
        : now.hour < 18
        ? 'Good afternoon'
        : 'Good evening';
    final date = MaterialLocalizations.of(context).formatFullDate(now);
    final hasAvatar = avatarUrl?.isNotEmpty == true;

    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.homeHeroStart, AppColors.homeHeroEnd],
        ),
        borderRadius: BorderRadius.circular(28),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.18),
            blurRadius: 28,
            offset: const Offset(0, 12),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(9),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.14),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Icon(
                  Icons.workspaces_rounded,
                  color: Colors.white,
                  size: 22,
                ),
              ),
              const SizedBox(width: 10),
              const Expanded(
                child: Text(
                  'WorkShift',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 21,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.6,
                  ),
                ),
              ),
              IconButton(
                tooltip: 'Logout',
                onPressed: onLogoutTap,
                icon: const Icon(
                  Icons.logout_rounded,
                  color: Colors.white70,
                  size: 20,
                ),
              ),
              CircleAvatar(
                radius: 21,
                backgroundColor: Colors.white.withValues(alpha: 0.18),
                backgroundImage: hasAvatar ? NetworkImage(avatarUrl!) : null,
                onBackgroundImageError: hasAvatar ? (_, _) {} : null,
                child: hasAvatar
                    ? null
                    : const Icon(
                        Icons.person_outline_rounded,
                        color: Colors.white,
                      ),
              ),
              if (onNotificationTap != null)
                Stack(
                  children: [
                    IconButton(
                      tooltip: 'Notification',
                      onPressed: onNotificationTap,
                      icon: const Icon(
                        Icons.notifications_none_rounded,
                        color: Colors.white70,
                        size: 20,
                      ),
                    ),
                    if (unreadCount > 0)
                      Positioned(
                        right: 2,
                        top: 2,
                        child: Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 5,
                            vertical: 1,
                          ),
                          constraints: const BoxConstraints(
                            minWidth: 18,
                            minHeight: 18,
                          ),
                          decoration: BoxDecoration(
                            color: AppColors.error,
                            borderRadius: BorderRadius.circular(9),
                            border: Border.all(
                              color: Colors.white,
                              width: 1.5,
                            ),
                          ),
                          child: Center(
                            child: Text(
                              unreadCount > 99 ? '99+' : '$unreadCount',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10,
                                fontWeight: FontWeight.w700,
                                height: 1,
                              ),
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
            ],
          ),
          const SizedBox(height: 28),
          Text(
            '$greeting,',
            style: const TextStyle(color: Colors.white70, fontSize: 15),
          ),
          const SizedBox(height: 5),
          Text(
            userName,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 30,
              height: 1.15,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.8,
            ),
          ),
          const SizedBox(height: 10),
          const Text(
            'Your day. Your shift. Let’s make it count.',
            style: TextStyle(color: Colors.white70, fontSize: 13, height: 1.5),
          ),
          const SizedBox(height: 22),
          Divider(color: Colors.white.withValues(alpha: 0.16), height: 1),
          const SizedBox(height: 16),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(
                Icons.storefront_outlined,
                color: AppColors.homeAccent,
                size: 18,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  branchName,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 12,
                    height: 1.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            date,
            style: const TextStyle(color: Colors.white70, fontSize: 12),
          ),
        ],
      ),
    );
  }
}
