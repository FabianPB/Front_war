import 'package:flutter/material.dart';

class StoreItemModel {
  const StoreItemModel({
    required this.category,
    required this.emoji,
    required this.name,
    required this.description,
    required this.rarity,
    required this.rarityColor,
    required this.price,
    this.badge,
  });

  final String category;
  final String emoji;
  final String name;
  final String description;
  final String rarity;
  final Color rarityColor;
  final String price;
  final String? badge;

  static const demoItems = [
    StoreItemModel(
      category: 'arma',
      badge: 'HOT',
      emoji: '🪓',
      name: 'Hacha de Guerra',
      description: 'Forjada en volcanes del norte. +45 daño físico.',
      rarity: '⬡ Legendario',
      rarityColor: Color(0xFFF59E0B),
      price: '850',
    ),
    StoreItemModel(
      category: 'arma',
      emoji: '🗡️',
      name: 'Machete Sangriento',
      description: 'Hoja curva impregnada con veneno antiguo.',
      rarity: '⬡ Épico',
      rarityColor: Color(0xFFA855F7),
      price: '620',
    ),
    StoreItemModel(
      category: 'arma',
      emoji: '🔱',
      name: 'Tridente Maldito',
      description: 'Tres puntas, tres almas atrapadas en acero.',
      rarity: '⬡ Legendario',
      rarityColor: Color(0xFFF59E0B),
      price: '1,200',
    ),
    StoreItemModel(
      category: 'defensa',
      emoji: '🛡️',
      name: 'Escudo Rúnico',
      description: 'Absorbe 30% del daño en cada impacto recibido.',
      rarity: '⬡ Raro',
      rarityColor: Color(0xFF3B82F6),
      price: '480',
    ),
    StoreItemModel(
      category: 'arma',
      badge: '-20%',
      emoji: '⚔️',
      name: 'Espada del Infierno',
      description: 'Arde en llamas eternas. Quema a cada impacto.',
      rarity: '⬡ Legendario',
      rarityColor: Color(0xFFF59E0B),
      price: '960',
    ),
    StoreItemModel(
      category: 'arma',
      emoji: '🪃',
      name: 'Bumerán de Hueso',
      description: 'Regresa al lanzador tras cada golpe certero.',
      rarity: '⬡ Raro',
      rarityColor: Color(0xFF3B82F6),
      price: '390',
    ),
    StoreItemModel(
      category: 'objeto',
      emoji: '💀',
      name: 'Amuleto Mortal',
      description: '+20% golpe crítico. Maldice con poder antiguo.',
      rarity: '⬡ Épico',
      rarityColor: Color(0xFFA855F7),
      price: '700',
    ),
    StoreItemModel(
      category: 'defensa',
      emoji: '⛓️',
      name: 'Cota de Malla Oscura',
      description: 'Armadura forjada en hierro maldito. +60 defensa.',
      rarity: '⬡ Épico',
      rarityColor: Color(0xFFA855F7),
      price: '780',
    ),
    StoreItemModel(
      category: 'objeto',
      emoji: '🧪',
      name: 'Poción de Sangre',
      description: 'Restaura 200 HP al instante. Sabor amargo.',
      rarity: '⬡ Común',
      rarityColor: Color.fromRGBO(200, 170, 170, 0.4),
      price: '80',
    ),
    StoreItemModel(
      category: 'objeto',
      badge: 'NUEVO',
      emoji: '🗺️',
      name: 'Mapa del Destino',
      description: 'Revela enemigos en un radio de 50 metros.',
      rarity: '⬡ Común',
      rarityColor: Color.fromRGBO(200, 170, 170, 0.4),
      price: '150',
    ),
  ];
}
