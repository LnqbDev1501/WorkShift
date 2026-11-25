import 'shift_registration_model.dart';

/// Shift data returned by available shifts, my schedule and shift detail APIs.
class AvailableShiftModel {
  final int shiftId;
  final int scheduleWeekId;
  final int branchId;
  final String branchName;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final DateTime registrationDeadline;
  final String status;
  final String scheduleWeekStatus;
  final String? note;
  final ShiftRegistrationModel? myRegistration;
  final bool canCancelRegistration;
  final String? cancellationBlockedReason;
  final bool isOpenShift;
  final List<ShiftRequirementModel> employeeRequirements;

  const AvailableShiftModel({
    required this.shiftId,
    required this.scheduleWeekId,
    required this.branchId,
    required this.branchName,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    required this.registrationDeadline,
    required this.status,
    required this.scheduleWeekStatus,
    this.note,
    this.myRegistration,
    this.canCancelRegistration = false,
    this.cancellationBlockedReason,
    this.isOpenShift = false,
    required this.employeeRequirements,
  });

  factory AvailableShiftModel.fromJson(Map<String, dynamic> json) {
    final weekStatus = json['scheduleWeekStatus'] as String? ?? '';
    return AvailableShiftModel(
      shiftId: (json['shiftId'] as num).toInt(),
      scheduleWeekId: (json['scheduleWeekId'] as num).toInt(),
      branchId: (json['branchId'] as num).toInt(),
      branchName: json['branchName'] as String,
      shiftName: json['shiftName'] as String,
      scheduledStartAt: DateTime.parse(
        json['scheduledStartAt'] as String,
      ).toUtc(),
      scheduledEndAt: DateTime.parse(json['scheduledEndAt'] as String).toUtc(),
      registrationDeadline: DateTime.parse(
        json['registrationDeadline'] as String,
      ).toUtc(),
      status: json['status'] as String,
      scheduleWeekStatus: weekStatus,
      note: json['note'] as String?,
      myRegistration: json['myRegistration'] == null
          ? null
          : ShiftRegistrationModel.fromJson(
              json['myRegistration'] as Map<String, dynamic>,
            ),
      canCancelRegistration: json['canCancelRegistration'] == true,
      cancellationBlockedReason: json['cancellationBlockedReason'] as String?,
      isOpenShift: json['isOpenShift'] == true,
      employeeRequirements: List.unmodifiable(
        (json['employeeRequirements'] as List).map(
          (item) =>
              ShiftRequirementModel.fromJson(item as Map<String, dynamic>),
        ),
      ),
    );
  }
}

class ShiftRequirementModel {
  final int shiftRequirementId;
  final int employeeTypeId;
  final String employeeTypeName;
  final int requiredCount;
  final int assignedCount;
  final int remainingCount;

  const ShiftRequirementModel({
    required this.shiftRequirementId,
    required this.employeeTypeId,
    required this.employeeTypeName,
    required this.requiredCount,
    required this.assignedCount,
    required this.remainingCount,
  });

  factory ShiftRequirementModel.fromJson(Map<String, dynamic> json) {
    return ShiftRequirementModel(
      shiftRequirementId: (json['shiftRequirementId'] as num).toInt(),
      employeeTypeId: (json['employeeTypeId'] as num).toInt(),
      employeeTypeName: json['employeeTypeName'] as String,
      requiredCount: (json['requiredCount'] as num).toInt(),
      assignedCount: (json['assignedCount'] as num).toInt(),
      remainingCount: (json['remainingCount'] as num).toInt(),
    );
  }
}

class AvailableShiftPage {
  final List<AvailableShiftModel> items;
  final int pageNumber;
  final int pageSize;
  final int totalItems;
  final int totalPages;

  const AvailableShiftPage({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalItems,
    required this.totalPages,
  });

  factory AvailableShiftPage.fromJson(Map<String, dynamic> json) {
    return AvailableShiftPage(
      items: List.unmodifiable(
        (json['items'] as List).map(
          (item) => AvailableShiftModel.fromJson(item as Map<String, dynamic>),
        ),
      ),
      pageNumber: (json['pageNumber'] as num).toInt(),
      pageSize: (json['pageSize'] as num).toInt(),
      totalItems: (json['totalItems'] as num).toInt(),
      totalPages: (json['totalPages'] as num).toInt(),
    );
  }
}
