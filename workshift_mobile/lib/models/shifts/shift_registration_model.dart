class ShiftAccessModel {
  final bool canManageShifts;
  final String? blockedReason;

  const ShiftAccessModel({required this.canManageShifts, this.blockedReason});

  static const checking = ShiftAccessModel(
    canManageShifts: false,
    blockedReason: 'Checking your employment contract. Please wait.',
  );

  static const unavailable = ShiftAccessModel(
    canManageShifts: false,
    blockedReason:
        'Unable to verify your employment contract. Refresh and try again.',
  );

  factory ShiftAccessModel.fromJson(Map<String, dynamic> json) =>
      ShiftAccessModel(
        canManageShifts: json['canManageShifts'] == true,
        blockedReason:
            json['blockedReason'] as String? ?? unavailable.blockedReason,
      );
}

class ShiftRegistrationModel {
  final int assignmentId;
  final int shiftId;
  final int? shiftRequirementId;
  final String assignmentSource;
  final String status;
  final DateTime assignedAt;
  final DateTime? cancelledAt;
  final String? cancelReason;

  const ShiftRegistrationModel({
    required this.assignmentId,
    required this.shiftId,
    this.shiftRequirementId,
    required this.assignmentSource,
    required this.status,
    required this.assignedAt,
    this.cancelledAt,
    this.cancelReason,
  });

  factory ShiftRegistrationModel.fromJson(Map<String, dynamic> json) =>
      ShiftRegistrationModel(
        assignmentId: (json['assignmentId'] as num).toInt(),
        shiftId: (json['shiftId'] as num).toInt(),
        shiftRequirementId: (json['shiftRequirementId'] as num?)?.toInt(),
        assignmentSource: json['assignmentSource'] as String,
        status: json['status'] as String,
        assignedAt: DateTime.parse(json['assignedAt'] as String).toUtc(),
        cancelledAt: json['cancelledAt'] == null
            ? null
            : DateTime.parse(json['cancelledAt'] as String).toUtc(),
        cancelReason: json['cancelReason'] as String?,
      );
}
