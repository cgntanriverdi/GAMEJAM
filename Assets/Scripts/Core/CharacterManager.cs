/// <summary>
/// Oyuncu karakter seçimini tutar. Dog varsayılan.
/// StartupMenuUI seçer; PlayerToken ve GridManager okur.
/// </summary>
public static class CharacterManager
{
    public enum CharacterType { Dog, Rabbit, Cat }

    public static CharacterType Current { get; private set; } = CharacterType.Dog;

    public static void Select(CharacterType c) { Current = c; }
}
