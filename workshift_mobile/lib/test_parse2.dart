import 'package:flutter/material.dart';

class TestView2 extends StatelessWidget {
  const TestView2({super.key});
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          if (true) ...[const Text('Test')],
        ],
      ),
    );
  }
}
