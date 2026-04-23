class Validators {
  static final RegExp _emailRegex = RegExp(r"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}");

  static String? validateRequired(
    String? value, {
    String fieldName = 'Este campo',
  }) {
    if (value == null || value.trim().isEmpty) {
      return '$fieldName es obligatorio.';
    }
    return null;
  }

  static String? validateUsername(String? value) {
    final requiredError = validateRequired(
      value,
      fieldName: 'Nombre de usuario',
    );
    if (requiredError != null) {
      return requiredError;
    }
    if (value!.trim().length < 3) {
      return 'El nombre de usuario debe tener al menos 3 caracteres.';
    }
    return null;
  }

  static String? validateEmail(String? value) {
    final requiredError = validateRequired(
      value,
      fieldName: 'Correo electrónico',
    );
    if (requiredError != null) {
      return requiredError;
    }
    if (!_emailRegex.hasMatch(value!.trim())) {
      return 'Introduce un correo electrónico válido.';
    }
    return null;
  }

  static String? validatePassword(String? value) {
    final requiredError = validateRequired(value, fieldName: 'Contraseña');
    if (requiredError != null) {
      return requiredError;
    }
    if (value!.length < 6) {
      return 'La contraseña debe tener al menos 6 caracteres.';
    }
    return null;
  }

  static String? validateDescription(String? value) {
    final requiredError = validateRequired(value, fieldName: 'Descripción');
    if (requiredError != null) {
      return requiredError;
    }
    if (value!.trim().length < 10) {
      return 'La descripción debe tener al menos 10 caracteres.';
    }
    return null;
  }
}
