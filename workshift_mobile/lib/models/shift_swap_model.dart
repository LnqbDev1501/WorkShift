class ShiftSwapAssignmentModel {
  final int assignmentId;
  final int shiftId;
  final int branchId;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;

  const ShiftSwapAssignmentModel({
    required this.assignmentId,
    required this.shiftId,
    required this.branchId,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
  });

  factory ShiftSwapAssignmentModel.fromJson(Map<String, dynamic> json) =>
      ShiftSwapAssignmentModel(
        assignmentId: (json['assignmentId'] as num).toInt(),
        shiftId: (json['shiftId'] as num).toInt(),
        branchId: (json['branchId'] as num).toInt(),
        shiftName: json['shiftName'] as String,
        scheduledStartAt: DateTime.parse(
          json['scheduledStartAt'] as String,
        ).toUtc(),
        scheduledEndAt: DateTime.parse(
          json['scheduledEndAt'] as String,
        ).toUtc(),
      );
}

class ShiftSwapModel {
  final int swapRequestId;
  final String requesterId;
  final String requesterName;
  final String targetUserId;
  final String targetUserName;
  final String status;
  final String? reason;
  final String? responseNote;
  final DateTime createdAt;
  final DateTime? respondedAt;
  final ShiftSwapAssignmentModel sourceAssignment;
  final ShiftSwapAssignmentModel targetAssignment;

  const ShiftSwapModel({
    required this.swapRequestId,
    required this.requesterId,
    required this.requesterName,
    required this.targetUserId,
    required this.targetUserName,
    required this.status,
    this.reason,
    this.responseNote,
    required this.createdAt,
    this.respondedAt,
    required this.sourceAssignment,
    required this.targetAssignment,
  });

  factory ShiftSwapModel.fromJson(Map<String, dynamic> json) => ShiftSwapModel(
    swapRequestId: (json['swapRequestId'] as num).toInt(),
    requesterId: json['requesterId'] as String,
    requesterName: json['requesterName'] as String,
    targetUserId: json['targetUserId'] as String,
    targetUserName: json['targetUserName'] as String,
    status: json['status'] as String,
    reason: json['reason'] as String?,
    responseNote: json['responseNote'] as String?,
    createdAt: DateTime.parse(json['createdAt'] as String).toUtc(),
    respondedAt: json['respondedAt'] == null
        ? null
        : DateTime.parse(json['respondedAt'] as String).toUtc(),
    sourceAssignment: ShiftSwapAssignmentModel.fromJson(
      json['sourceAssignment'] as Map<String, dynamic>,
    ),
    targetAssignment: ShiftSwapAssignmentModel.fromJson(
      json['targetAssignment'] as Map<String, dynamic>,
    ),
  );
}

class ShiftSwapPage {
  final List<ShiftSwapModel> items;
  final int totalItems;
  final int totalPages;
  const ShiftSwapPage({
    required this.items,
    required this.totalItems,
    required this.totalPages,
  });
  factory ShiftSwapPage.fromJson(Map<String, dynamic> json) => ShiftSwapPage(
    items: List.unmodifiable(
      (json['items'] as List).map(
        (item) => ShiftSwapModel.fromJson(item as Map<String, dynamic>),
      ),
    ),
    totalItems: (json['totalItems'] as num).toInt(),
    totalPages: (json['totalPages'] as num).toInt(),
  );
}

class ShiftSwapCandidateModel {
  final String staffId;
  final String staffName;
  final ShiftSwapAssignmentModel assignment;
  const ShiftSwapCandidateModel({
    required this.staffId,
    required this.staffName,
    required this.assignment,
  });
  factory ShiftSwapCandidateModel.fromJson(Map<String, dynamic> json) =>
      ShiftSwapCandidateModel(
        staffId: json['staffId'] as String,
        staffName: json['staffName'] as String,
        assignment: ShiftSwapAssignmentModel.fromJson(
          json['assignment'] as Map<String, dynamic>,
        ),
      );
}
