class UserModel {
  const UserModel({
    required this.username,
    required this.email,
    required this.password,
  });

  final String username;
  final String email;
  final String password;

  Map<String, Object?> toPlayerData() {
    return {'username': username, 'email': email, 'provider': 'password'};
  }
}
