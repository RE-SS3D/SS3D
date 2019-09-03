using System;

[Flags]
public enum SlotTypes
{
    LeftHand = 1,
    RightHand = 2,
    Helmet = 4,
    Gloves = 8,
    Ears = 16,
    SuitStorage = 32,
    Vest = 64,
    Shoes = 128,
    Glasses = 256,
    Mask = 512,
    Shirt = 1024,
    Card = 2048,
    Belt = 4096,
    Backpack = 8192,
    LeftPocket = 16384,
    RightPocket = 32768,
    Storage = 65536
}