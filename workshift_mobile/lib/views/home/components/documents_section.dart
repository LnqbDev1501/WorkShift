import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';

import '../../contracts/staff_contract_list_view.dart';

/// Component: Mục "Hồ sơ & Chứng từ" (Phiếu lương, Hợp đồng lao động)
class DocumentsSection extends StatelessWidget {
  const DocumentsSection({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Documents & Records',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 12),

        // Payslip card
        _buildDocumentItem(
          icon: Icons.payments_outlined,
          iconColor: AppColors.primary,
          iconBgColor: AppColors.primaryLight,
          title: 'Payslip',
          subtitle: 'View official issued payslips',
          onTap: () {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Opening Payslip list')),
            );
          },
        ),
        const SizedBox(height: 10),

        // Employment contract card
        _buildDocumentItem(
          icon: Icons.verified_user_outlined,
          iconColor: AppColors.success,
          iconBgColor: AppColors.successBg,
          title: 'Employment Contract',
          subtitle: 'View, accept and download electronic contracts',
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const StaffContractListView()),
            );
          },
        ),
      ],
    );
  }

  Widget _buildDocumentItem({
    required IconData icon,
    required Color iconColor,
    required Color iconBgColor,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.borderLight),
        ),
        child: Row(
          children: [
            // Ô Icon vuông bo góc
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: iconBgColor,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: iconColor, size: 22),
            ),
            const SizedBox(width: 14),

            // Tiêu đề & mô tả
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    subtitle,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textMuted,
                    ),
                  ),
                ],
              ),
            ),

            // Mũi tên
            const Icon(Icons.chevron_right, color: AppColors.textMuted),
          ],
        ),
      ),
    );
  }
}
