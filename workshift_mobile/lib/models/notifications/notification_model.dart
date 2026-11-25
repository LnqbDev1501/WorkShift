/// Thông báo cá nhân của nhân viên (khớp với PersonalNotificationListResponse/DetailResponse từ Backend .NET)
class NotificationModel {
  final int userNotificationId;
  final int notificationId;
  final String title;
  final String message;
  final String notificationType;
  final String audience;
  final List<String> channels;
  final DateTime createdAt;
  final bool isRead;
  final DateTime? readAt;
  final int page;
  final int pageSize;
  final int totalCount;
  final int? targetRegionId;
  final int? targetBranchId;
  final List<String> targetRoles;
  final String? createdByName;
  final int inAppRecipientCount;

  NotificationModel({
    required this.userNotificationId,
    required this.notificationId,
    required this.title,
    required this.message,
    required this.notificationType,
    required this.audience,
    required this.channels,
    required this.createdAt,
    required this.isRead,
    this.readAt,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    this.targetRegionId,
    this.targetBranchId,
    required this.targetRoles,
    this.createdByName,
    required this.inAppRecipientCount,
  });

  factory NotificationModel.fromJson(Map<String, dynamic> json) {
    return NotificationModel(
      userNotificationId: json['userNotificationId']?.toInt() ?? 0,
      notificationId: json['notificationId']?.toInt() ?? 0,
      title: json['title']?.toString() ?? '',
      message: json['message']?.toString() ?? '',
      notificationType: json['notificationType']?.toString() ?? '',
      audience: json['audience']?.toString() ?? '',
      channels:
          (json['channels'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'].toString())
          : DateTime.now(),
      isRead: json['isRead'] == true,
      readAt: json['readAt'] != null
          ? DateTime.parse(json['readAt'].toString())
          : null,
      page: json['page']?.toInt() ?? 1,
      pageSize: json['pageSize']?.toInt() ?? 20,
      totalCount: json['totalCount']?.toInt() ?? 0,
      targetRegionId: json['targetRegionId']?.toInt(),
      targetBranchId: json['targetBranchId']?.toInt(),
      targetRoles:
          (json['targetRoles'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      createdByName: json['createdByName']?.toString(),
      inAppRecipientCount: json['inAppRecipientCount']?.toInt() ?? 0,
    );
  }
}
