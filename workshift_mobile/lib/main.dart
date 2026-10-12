import 'package:flutter/material.dart';
import 'theme/app_theme.dart';
import 'views/auth/login_view.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const WorkShiftApp());
}

class WorkShiftApp extends StatelessWidget {
  const WorkShiftApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'WorkShift',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      home: const LoginView(),
    );
  }
}
