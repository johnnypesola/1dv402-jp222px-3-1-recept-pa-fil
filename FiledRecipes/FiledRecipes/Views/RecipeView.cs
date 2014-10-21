using FiledRecipes.Domain;
using FiledRecipes.App.Mvp;
using FiledRecipes.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiledRecipes.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class RecipeView : ViewBase, IRecipeView
    {
        public void Show(IRecipe recipe)
        {
            int instructionCount = 1;

            Console.Clear();

            // Set recipe name as header
            Header = recipe.Name;

            // Display recipe header panel
            ShowHeaderPanel();

            // Display ingredients
            this.ShowPanel("Ingredienser", App.Controls.MessagePanelOptions.Basic);

            foreach(Ingredient ingredient in recipe.Ingredients)
            {
                Console.WriteLine(String.Format(" {0,4} {1,-6} {2,0}", ingredient.Amount, ingredient.Measure, ingredient.Name));
            }

            // Display instructions
            this.ShowPanel("Instruktioner", App.Controls.MessagePanelOptions.Basic);
            
            foreach (string instruction in recipe.Instructions)
            {
                System.Console.WriteLine("({0})", instructionCount);
                Console.WriteLine(instruction);

                instructionCount++;
            }
        }

        public void Show(IEnumerable<IRecipe> recipes)
        {
            foreach(IRecipe recipe in recipes)
            {
                Show(recipe);
                ContinueOnKeyPressed();
            }
        }
    }
}
