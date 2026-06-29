namespace GWBGameJam
{
    public readonly struct OnIngredientUsed
    {
        public readonly IngredientType Ingredient;

        public OnIngredientUsed(IngredientType ingredient)
        {
            Ingredient = ingredient;
        }
    }
}
