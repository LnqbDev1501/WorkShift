import 'package:flutter/material.dart';
import '../../controllers/auth_controller.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_text_field.dart';
import '../home/home_view.dart';
import 'forgot_password_view.dart';

/// Màn hình Đăng nhập (Login View)
class LoginView extends StatefulWidget {
  const LoginView({super.key});

  @override
  State<LoginView> createState() => _LoginViewState();
}

class _LoginViewState extends State<LoginView> with SingleTickerProviderStateMixin {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _authController = AuthController();

  late AnimationController _animationController;
  late Animation<double> _fadeAnimation;
  late Animation<Offset> _slideAnimation;

  @override
  void initState() {
    super.initState();
    _authController.addListener(_onControllerUpdate);

    _animationController = AnimationController(
      duration: const Duration(milliseconds: 800),
      vsync: this,
    );

    _fadeAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(parent: _animationController, curve: Curves.easeOutCubic),
    );

    _slideAnimation = Tween<Offset>(
      begin: const Offset(0, 0.15),
      end: Offset.zero,
    ).animate(CurvedAnimation(parent: _animationController, curve: Curves.easeOutCubic));

    _animationController.forward();
  }

  @override
  void dispose() {
    _authController.removeListener(_onControllerUpdate);
    _emailController.dispose();
    _passwordController.dispose();
    _animationController.dispose();
    super.dispose();
  }

  void _onControllerUpdate() {
    if (mounted) setState(() {});
  }

  Future<void> _handleLogin() async {
    FocusScope.of(context).unfocus();

    if (!_formKey.currentState!.validate()) {
      return;
    }

    final success = await _authController.login(
      email: _emailController.text,
      password: _passwordController.text,
    );

    if (success && mounted) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (_) => HomeView(
            userName: _authController.currentUser?.fullName,
          ),
        ),
      );
    }
  }

  void _goToForgotPassword() {
    Navigator.push(
      context,
      PageRouteBuilder(
        pageBuilder: (context, animation, secondaryAnimation) => const ForgotPasswordView(),
        transitionsBuilder: (context, animation, secondaryAnimation, child) {
          return SlideTransition(
            position: Tween<Offset>(
              begin: const Offset(1.0, 0.0),
              end: Offset.zero,
            ).animate(CurvedAnimation(parent: animation, curve: Curves.easeInOutCubic)),
            child: FadeTransition(opacity: animation, child: child),
          );
        },
        transitionDuration: const Duration(milliseconds: 300),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.of(context).padding.top;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: GestureDetector(
        onTap: () => FocusScope.of(context).unfocus(),
        behavior: HitTestBehavior.opaque,
        child: Stack(
          children: [
            // 1. Ambient Brand Hero Header (Pure Flutter vector & gradients - NO images)
            Positioned(
              top: 0,
              left: 0,
              right: 0,
              height: 290 + topPadding,
              child: Container(
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [AppColors.homeHeroStart, AppColors.homeHeroEnd],
                  ),
                  borderRadius: BorderRadius.only(
                    bottomLeft: Radius.circular(36),
                    bottomRight: Radius.circular(36),
                  ),
                ),
                child: Stack(
                  children: [
                    // Ambient decorative circles for modern visual depth
                    Positioned(
                      top: -40,
                      right: -30,
                      child: Container(
                        width: 190,
                        height: 190,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: Colors.white.withValues(alpha: 0.06),
                        ),
                      ),
                    ),
                    Positioned(
                      bottom: 30,
                      left: -40,
                      child: Container(
                        width: 140,
                        height: 140,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: AppColors.homeAccent.withValues(alpha: 0.12),
                        ),
                      ),
                    ),
                    Positioned(
                      top: topPadding + 40,
                      right: 40,
                      child: Container(
                        width: 70,
                        height: 70,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: Colors.white.withValues(alpha: 0.04),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // 2. Main Scrollable Content
            SafeArea(
              child: SingleChildScrollView(
                physics: const BouncingScrollPhysics(),
                keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                child: FadeTransition(
                  opacity: _fadeAnimation,
                  child: SlideTransition(
                    position: _slideAnimation,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const SizedBox(height: 14),

                        // Brand Hero & Logo Section
                        Center(
                          child: Column(
                            children: [
                              // App Logo Badge
                              TweenAnimationBuilder<double>(
                                tween: Tween(begin: 0.0, end: 1.0),
                                duration: const Duration(milliseconds: 900),
                                curve: Curves.elasticOut,
                                builder: (_, value, child) {
                                  return Transform.scale(
                                    scale: value,
                                    child: child,
                                  );
                                },
                                child: Container(
                                  width: 76,
                                  height: 76,
                                  decoration: BoxDecoration(
                                    color: Colors.white,
                                    borderRadius: BorderRadius.circular(22),
                                    boxShadow: [
                                      BoxShadow(
                                        color: Colors.black.withValues(alpha: 0.16),
                                        blurRadius: 24,
                                        offset: const Offset(0, 10),
                                      ),
                                    ],
                                  ),
                                  child: Center(
                                    child: Container(
                                      width: 56,
                                      height: 56,
                                      decoration: BoxDecoration(
                                        gradient: const LinearGradient(
                                          colors: [AppColors.homeHeroStart, AppColors.homeHeroEnd],
                                          begin: Alignment.topLeft,
                                          end: Alignment.bottomRight,
                                        ),
                                        borderRadius: BorderRadius.circular(16),
                                      ),
                                      child: const Icon(
                                        Icons.access_time_filled_rounded,
                                        color: Colors.white,
                                        size: 32,
                                      ),
                                    ),
                                  ),
                                ),
                              ),
                              const SizedBox(height: 14),

                              // App Name
                              const Text(
                                'WorkShift',
                                style: TextStyle(
                                  fontSize: 30,
                                  fontWeight: FontWeight.w900,
                                  color: Colors.white,
                                  letterSpacing: -0.6,
                                ),
                              ),
                              const SizedBox(height: 8),

                              // Staff Portal Pill Badge
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                                decoration: BoxDecoration(
                                  color: Colors.white.withValues(alpha: 0.16),
                                  borderRadius: BorderRadius.circular(20),
                                  border: Border.all(
                                    color: Colors.white.withValues(alpha: 0.25),
                                    width: 1,
                                  ),
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Container(
                                      width: 6,
                                      height: 6,
                                      decoration: const BoxDecoration(
                                        color: AppColors.homeAccent,
                                        shape: BoxShape.circle,
                                      ),
                                    ),
                                    const SizedBox(width: 6),
                                    const Text(
                                      'STAFF WORKFORCE PORTAL',
                                      style: TextStyle(
                                        color: Colors.white,
                                        fontSize: 11,
                                        fontWeight: FontWeight.w700,
                                        letterSpacing: 0.8,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),

                        const SizedBox(height: 26),

                        // Login Form Card
                        Container(
                          padding: const EdgeInsets.all(24),
                          decoration: BoxDecoration(
                            color: AppColors.surface,
                            borderRadius: BorderRadius.circular(24),
                            border: Border.all(color: AppColors.borderLight, width: 1.2),
                            boxShadow: [
                              BoxShadow(
                                color: AppColors.homeHeroStart.withValues(alpha: 0.08),
                                blurRadius: 28,
                                offset: const Offset(0, 12),
                                spreadRadius: -4,
                              ),
                              BoxShadow(
                                color: Colors.black.withValues(alpha: 0.02),
                                blurRadius: 8,
                                offset: const Offset(0, 2),
                              ),
                            ],
                          ),
                          child: Form(
                            key: _formKey,
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                const Text(
                                  'Sign In',
                                  style: TextStyle(
                                    fontSize: 22,
                                    fontWeight: FontWeight.w800,
                                    color: AppColors.textPrimary,
                                    letterSpacing: -0.3,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                const Text(
                                  'Sign in with your staff account to manage shifts and attendance.',
                                  style: TextStyle(
                                    fontSize: 13.5,
                                    color: AppColors.textSecondary,
                                    height: 1.4,
                                  ),
                                ),
                                const SizedBox(height: 20),

                                // Error notification banner
                                AnimatedSwitcher(
                                  duration: const Duration(milliseconds: 300),
                                  switchInCurve: Curves.easeOutCubic,
                                  switchOutCurve: Curves.easeInCubic,
                                  child: _authController.errorMessage != null
                                      ? Container(
                                          key: const ValueKey('error'),
                                          margin: const EdgeInsets.only(bottom: 18),
                                          padding: const EdgeInsets.all(14),
                                          decoration: BoxDecoration(
                                            color: AppColors.errorBg,
                                            borderRadius: BorderRadius.circular(14),
                                            border: Border.all(
                                              color: AppColors.error.withValues(alpha: 0.35),
                                            ),
                                          ),
                                          child: Row(
                                            crossAxisAlignment: CrossAxisAlignment.start,
                                            children: [
                                              Container(
                                                padding: const EdgeInsets.all(6),
                                                decoration: BoxDecoration(
                                                  color: AppColors.error.withValues(alpha: 0.15),
                                                  shape: BoxShape.circle,
                                                ),
                                                child: const Icon(
                                                  Icons.error_outline_rounded,
                                                  color: AppColors.error,
                                                  size: 18,
                                                ),
                                              ),
                                              const SizedBox(width: 10),
                                              Expanded(
                                                child: Text(
                                                  _authController.errorMessage!,
                                                  style: const TextStyle(
                                                    color: AppColors.error,
                                                    fontSize: 13.5,
                                                    fontWeight: FontWeight.w500,
                                                    height: 1.35,
                                                  ),
                                                ),
                                              ),
                                            ],
                                          ),
                                        )
                                      : const SizedBox.shrink(),
                                ),

                                // Email input
                                _AnimatedTextField(
                                  delay: 100,
                                  child: AppTextField(
                                    label: 'Email Address',
                                    hintText: 'you@company.com',
                                    controller: _emailController,
                                    keyboardType: TextInputType.emailAddress,
                                    prefixIcon: Icons.alternate_email_rounded,
                                    onChanged: (_) => _authController.clearError(),
                                    validator: (val) {
                                      if (val == null || val.trim().isEmpty) {
                                        return 'Please enter your email';
                                      }
                                      if (!val.contains('@') || !val.contains('.')) {
                                        return 'Please enter a valid email address';
                                      }
                                      return null;
                                    },
                                  ),
                                ),
                                const SizedBox(height: 18),

                                // Password input
                                _AnimatedTextField(
                                  delay: 180,
                                  child: AppTextField(
                                    label: 'Password',
                                    hintText: 'Enter your password',
                                    controller: _passwordController,
                                    isPassword: true,
                                    prefixIcon: Icons.lock_outline_rounded,
                                    onChanged: (_) => _authController.clearError(),
                                    validator: (val) {
                                      if (val == null || val.trim().isEmpty) {
                                        return 'Please enter your password';
                                      }
                                      if (val.length < 6) {
                                        return 'Password must be at least 6 characters';
                                      }
                                      return null;
                                    },
                                  ),
                                ),
                                const SizedBox(height: 10),

                                // Forgot Password link
                                _AnimatedTextField(
                                  delay: 240,
                                  child: Align(
                                    alignment: Alignment.centerRight,
                                    child: TextButton(
                                      onPressed: _goToForgotPassword,
                                      style: TextButton.styleFrom(
                                        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 2),
                                        minimumSize: Size.zero,
                                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                        foregroundColor: AppColors.primary,
                                      ),
                                      child: const Text(
                                        'Forgot Password?',
                                        style: TextStyle(
                                          fontSize: 13.5,
                                          fontWeight: FontWeight.w600,
                                        ),
                                      ),
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 18),

                                // Sign In button
                                _AnimatedTextField(
                                  delay: 300,
                                  child: AppButton(
                                    text: 'Sign In',
                                    icon: Icons.login_rounded,
                                    isLoading: _authController.isLoading,
                                    onPressed: _handleLogin,
                                    height: 52,
                                    borderRadius: 14,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),

                        const SizedBox(height: 28),

                        // Enterprise Security & Version Footer
                        _AnimatedTextField(
                          delay: 360,
                          child: Column(
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: const [
                                  Icon(
                                    Icons.verified_user_outlined,
                                    size: 15,
                                    color: AppColors.textMuted,
                                  ),
                                  SizedBox(width: 6),
                                  Text(
                                    'WorkShift Secure Authentication',
                                    style: TextStyle(
                                      fontSize: 12,
                                      color: AppColors.textMuted,
                                      fontWeight: FontWeight.w500,
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 6),
                              Text(
                                'WorkShift Management System v1.0',
                                style: TextStyle(
                                  fontSize: 11.5,
                                  color: AppColors.textMuted.withValues(alpha: 0.8),
                                  letterSpacing: 0.2,
                                ),
                              ),
                            ],
                          ),
                        ),

                        const SizedBox(height: 20),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AnimatedTextField extends StatefulWidget {
  final Widget child;
  final int delay;

  const _AnimatedTextField({required this.child, required this.delay});

  @override
  State<_AnimatedTextField> createState() => _AnimatedTextFieldState();
}

class _AnimatedTextFieldState extends State<_AnimatedTextField>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _fade;
  late Animation<Offset> _slide;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      duration: const Duration(milliseconds: 600),
      vsync: this,
    );
    _fade = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic),
    );
    _slide = Tween<Offset>(
      begin: const Offset(0, 0.2),
      end: Offset.zero,
    ).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic));

    Future.delayed(Duration(milliseconds: widget.delay), () {
      if (mounted) _controller.forward();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
      opacity: _fade,
      child: SlideTransition(position: _slide, child: widget.child),
    );
  }
}