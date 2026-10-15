import 'package:flutter_test/flutter_test.dart';
import 'package:workshift_mobile/controllers/submit_shift_swap_controller.dart';
import 'package:workshift_mobile/models/available_shift_model.dart';
import 'package:workshift_mobile/models/shift_registration_model.dart';
import 'package:workshift_mobile/services/my_shifts_service.dart';

class _Schedule extends MyShiftsService {
  final List<AvailableShiftModel> items;
  _Schedule(this.items);
  @override
  Future<AvailableShiftPage> getMyShifts({
    int pageNumber = 1,
    required DateTime fromDate,
    DateTime? toDate,
  }) async => AvailableShiftPage(
    items: items,
    pageNumber: 1,
    pageSize: 20,
    totalItems: items.length,
    totalPages: 1,
  );
}

void main() {
  final now = DateTime.utc(2026, 10, 15, 6);
  AvailableShiftModel shift(
    int id,
    Duration lead, {
    String assignmentStatus = 'ASSIGNED',
    String status = 'SCHEDULED',
    String weekStatus = 'REGISTRATION_CLOSED',
  }) {
    final start = now.add(lead);
    return AvailableShiftModel(
      shiftId: id,
      scheduleWeekId: 1,
      branchId: 1,
      branchName: 'Branch',
      shiftName: 'Morning',
      scheduledStartAt: start,
      scheduledEndAt: start.add(const Duration(hours: 5)),
      registrationDeadline: now,
      status: status,
      scheduleWeekStatus: weekStatus,
      employeeRequirements: const [],
      myRegistration: ShiftRegistrationModel(
        assignmentId: id,
        shiftId: id,
        assignmentSource: 'MANAGER_ASSIGNED',
        status: assignmentStatus,
        assignedAt: now,
      ),
    );
  }

  test(
    'Only assigned scheduled shifts respect the 24-hour boundary; completed shifts are excluded',
    () async {
      final controller = SubmitShiftSwapController(
        now: () => now,
        scheduleService: _Schedule([
          shift(1, const Duration(hours: 24)),
          shift(2, const Duration(hours: 24, microseconds: -1)),
          shift(3, const Duration(hours: 25), status: 'COMPLETED'),
          shift(4, const Duration(hours: 25), assignmentStatus: 'COMPLETED'),
          shift(5, const Duration(hours: -1)),
          shift(6, const Duration(hours: 25), assignmentStatus: 'CANCELLED'),
          shift(7, const Duration(hours: 25), status: 'CANCELLED'),
          shift(8, const Duration(hours: 25), weekStatus: 'DRAFT'),
        ]),
      );
      addTearDown(controller.dispose);
      await controller.loadSources();
      expect(controller.sources.map((s) => s.shiftId), [1]);
      expect(controller.error, isNull);
    },
  );
}
