import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:workshift_mobile/models/staff_request_model.dart';
import 'package:workshift_mobile/views/home/components/documents_section.dart';
import 'package:workshift_mobile/views/home/components/pending_tasks.dart';
import 'package:workshift_mobile/views/home/components/quick_actions.dart';

void main() {
  Widget screen(Widget child) => MaterialApp(
    home: Scaffold(body: SingleChildScrollView(child: child)),
  );

  testWidgets(
    'Recent requests show server status and open the selected request',
    (tester) async {
      tester.view.physicalSize = const Size(320, 900);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final request = StaffEmployeeRequestListItemModel(
        requestId: 42,
        userId: 'staff',
        branchId: 1,
        branchName: 'Branch',
        branchCode: 'B1',
        requestType: 'LEAVE',
        status: 'APPROVED',
        createdAt: DateTime(2026, 10, 1),
        updatedAt: DateTime(2026, 10, 15, 9),
      );
      int? selectedId;
      await tester.pumpWidget(
        screen(
          PendingTasks(
            requests: [request],
            onRequestTap: (item) => selectedId = item.requestId,
            onViewAll: () {},
          ),
        ),
      );
      expect(find.text('Đã duyệt'), findsOneWidget);
      await tester.tap(find.textContaining('#42'));
      expect(selectedId, 42);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('Request load failure does not appear as an empty inbox', (
    tester,
  ) async {
    await tester.pumpWidget(
      screen(
        PendingTasks(
          requests: const [],
          errorMessage: 'Unable to load requests',
          onRequestTap: (_) {},
          onViewAll: () {},
        ),
      ),
    );
    expect(find.text('Unable to load requests'), findsOneWidget);
    expect(find.text('No requests yet'), findsNothing);
  });

  testWidgets(
    'Weekly card displays exact hours and minutes, and hides stale totals on error',
    (tester) async {
      Widget weekly({String? error}) => screen(
        DocumentsSection(
          workingMinutes: 1255,
          weekStart: DateTime(2026, 10, 12),
          weekEnd: DateTime(2026, 10, 18),
          errorMessage: error,
          onViewHistory: () {},
        ),
      );
      await tester.pumpWidget(weekly());
      expect(find.text('20h 55m'), findsOneWidget);
      await tester.pumpWidget(weekly(error: 'Unable to load hours'));
      expect(find.text('20h 55m'), findsNothing);
      expect(find.text('Unable to load hours'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'See more follows quick action tiles and expands the remaining actions',
    (tester) async {
      await tester.pumpWidget(screen(const QuickActions()));
      expect(
        tester.getTopLeft(find.text('See more')).dy,
        greaterThan(tester.getTopLeft(find.text('Order History')).dy),
      );
      await tester.ensureVisible(find.text('See more'));
      await tester.tap(find.text('See more'));
      await tester.pump();
      expect(find.text('Contract'), findsOneWidget);
      expect(find.text('Collapse'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
