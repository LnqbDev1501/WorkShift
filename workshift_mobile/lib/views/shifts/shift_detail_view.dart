import 'package:flutter/material.dart';
import '../../controllers/shift_detail_controller.dart';
import '../../widgets/app_state_panel.dart';
import 'components/shift_detail_content.dart';

class ShiftDetailView extends StatefulWidget {
  final int shiftId;
  final ShiftDetailController? controller;
  const ShiftDetailView({super.key, required this.shiftId, this.controller});
  @override
  State<ShiftDetailView> createState() => _ShiftDetailViewState();
}

class _ShiftDetailViewState extends State<ShiftDetailView>
    with WidgetsBindingObserver {
  late final ShiftDetailController _controller;
  @override
  void initState() {
    super.initState();
    _controller =
        widget.controller ?? ShiftDetailController(shiftId: widget.shiftId);
    WidgetsBinding.instance.addObserver(this);
    _controller.refresh();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) _controller.refresh();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    if (widget.controller == null) _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: _controller,
    builder: (context, _) => Scaffold(
      appBar: AppBar(
        title: const Text('Shift Details'),
        leading: const BackButton(),
        actions: [
          IconButton(
            tooltip: 'Refresh details',
            onPressed: _controller.isLoading ? null : _controller.refresh,
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: _controller.refresh,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              if (_controller.isLoading)
                _controller.shift == null
                    ? const Padding(
                        padding: EdgeInsets.all(64),
                        child: Center(child: CircularProgressIndicator()),
                      )
                    : const LinearProgressIndicator(minHeight: 2),
              if (_controller.error != null)
                AppStatePanel(
                  icon: Icons.info_outline,
                  title: 'Couldn’t load shift details',
                  message: _controller.error!,
                  actionLabel: 'Try again',
                  onAction: _controller.refresh,
                  isError: true,
                ),
              if (_controller.shift != null)
                ShiftDetailContent(shift: _controller.shift!),
            ],
          ),
        ),
      ),
    ),
  );
}
