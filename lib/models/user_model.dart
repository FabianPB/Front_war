class UserModel {
  const UserModel({
    required this.username,
    required this.email,
    required this.password,
    this.photoPath,
  });

  final String username;
  final String email;
  final String password;
  final String? photoPath;

  Map<String, Object?> toPlayerData() {
    return {'username': username, 'email': email, 'provider': 'password'};
  }

  UserModel copyWith({
    String? username,
    String? email,
    String? password,
    String? photoPath,
  }) {
    return UserModel(
      username: username ?? this.username,
      email: email ?? this.email,
      password: password ?? this.password,
      photoPath: photoPath ?? this.photoPath,
    );
  }
}
