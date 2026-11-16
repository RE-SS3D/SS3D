---
name: clothing world presentation
overview: One clothing Item NetworkObject; folded child for world/hand; ClothesDisplayer for worn body mesh.
todos:
  - id: presentation-component
    content: Add ClothingItemPresentation and hook Item.SetVisibility
    status: completed
  - id: collider-prefab
    content: Root BoxCollider on JumpsuitGrey + ClothingPrefabSetup; retire JumpsuitSecurityFolded
    status: completed
  - id: docs-verify
    content: Sync inventory/combat system maps
    status: completed
---

# Clothing folded vs worn presentation

Shipped: single-NO world form via `ClothingItemPresentation` on jumpsuit base (`JumpsuitGrey`); variants inherit. Orphan `JumpsuitSecurityFolded` removed from content, Addressables, and `DefaultPrefabObjects`.

Follow-ups shipped: `spawndummy` equips `JumpsuitSecurity`; equipment-doll HUD uses worn-shaped icon via `Item.GetHudSprite(preferWornShape: true)`.

Armor Phase 5 still uses `JumpsuitSecurity` + `ArmorItemExtension` on that same Item.
