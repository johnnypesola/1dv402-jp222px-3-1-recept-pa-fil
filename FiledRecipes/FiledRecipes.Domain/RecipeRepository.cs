using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiledRecipes.Domain
{
    /// <summary>
    /// Holder for recipes.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        /// <summary>
        /// Represents the recipe section.
        /// </summary>
        private const string SectionRecipe = "[Recept]";

        /// <summary>
        /// Represents the ingredients section.
        /// </summary>
        private const string SectionIngredients = "[Ingredienser]";

        /// <summary>
        /// Represents the instructions section.
        /// </summary>
        private const string SectionInstructions = "[Instruktioner]";

        /// <summary>
        /// Occurs after changes to the underlying collection of recipes.
        /// </summary>
        public event EventHandler RecipesChangedEvent;

        /// <summary>
        /// Specifies how the next line read from the file will be interpreted.
        /// </summary>
        private enum RecipeReadStatus { Indefinite, New, Ingredient, Instruction };

        /// <summary>
        /// Collection of recipes.
        /// </summary>
        private List<IRecipe> _recipes;

        /// <summary>
        /// The fully qualified path and name of the file with recipes.
        /// </summary>
        private string _path;

        /// <summary>
        /// Indicates whether the collection of recipes has been modified since it was last saved.
        /// </summary>
        public bool IsModified { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the RecipeRepository class.
        /// </summary>
        /// <param name="path">The path and name of the file with recipes.</param>
        public RecipeRepository(string path)
        {
            // Throws an exception if the path is invalid.
            _path = Path.GetFullPath(path);

            _recipes = new List<IRecipe>();
        }

        /// <summary>
        /// Returns a collection of recipes.
        /// </summary>
        /// <returns>A IEnumerable&lt;Recipe&gt; containing all the recipes.</returns>
        public virtual IEnumerable<IRecipe> GetAll()
        {
            // Deep copy the objects to avoid privacy leaks.
            return _recipes.Select(r => (IRecipe)r.Clone());
        }

        /// <summary>
        /// Returns a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to get.</param>
        /// <returns>The recipe at the specified index.</returns>
        public virtual IRecipe GetAt(int index)
        {
            // Deep copy the object to avoid privacy leak.
            return (IRecipe)_recipes[index].Clone();
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to delete. The value can be null.</param>
        public virtual void Delete(IRecipe recipe)
        {
            // If it's a copy of a recipe...
            if (!_recipes.Contains(recipe))
            {
                // ...try to find the original!
                recipe = _recipes.Find(r => r.Equals(recipe));
            }
            _recipes.Remove(recipe);
            IsModified = true;
            OnRecipesChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to delete.</param>
        public virtual void Delete(int index)
        {
            Delete(_recipes[index]);
        }

        /// <summary>
        /// Raises the RecipesChanged event.
        /// </summary>
        /// <param name="e">The EventArgs that contains the event data.</param>
        protected virtual void OnRecipesChanged(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler handler = RecipesChangedEvent;

            // Event will be null if there are no subscribers. 
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }


        public void Load()
        {
            List<IRecipe> recipes = new List<IRecipe>(10);
            RecipeReadStatus recipeReadStatus = RecipeReadStatus.Indefinite;
            Ingredient ingredients = new Ingredient();
            string[] splittedLine;
            string line;

            try
            {
                // Open file, automaticly close it afterwards
                using(StreamReader fileStream = new StreamReader(@"..\..\App_Data\Recipes.txt"))
                {
                    // Read lines while there are lines to read in the file.
                    while((line = fileStream.ReadLine()) != null)
                    {
                        // Skip line if its empty
                        if (line.Length <= 1) continue;

                        // Check if we are entering a new section
                        if (line == SectionRecipe)
                        {
                            recipeReadStatus = RecipeReadStatus.New;
                        }
                        else if (line == SectionIngredients)
                        {
                            recipeReadStatus = RecipeReadStatus.Ingredient;
                        }
                        else if (line == SectionInstructions)
                        {
                            recipeReadStatus = RecipeReadStatus.Instruction;
                        }

                        // Its in a section with values (line contains values)
                        else
                        {
                            if (recipeReadStatus == RecipeReadStatus.New)
                            {
                                recipes.Add(new Recipe(line));
                                // recipeReadStatus = RecipeReadStatus.Indefinite;
                            }
                            else if (recipeReadStatus == RecipeReadStatus.Ingredient)
                            {
                                // Split line and check count.
                                if( ( splittedLine = line.Split(';') ).Length == 3 )
                                {
                                    // Assign values
                                    ingredients.Amount = splittedLine[0];
                                    ingredients.Measure = splittedLine[1];
                                    ingredients.Name = splittedLine[2];

                                    recipes.Last().Add(ingredients);
                                }
                                else
                                {
                                    throw new FileFormatException("Could not parse file. Row contains wrong number of values.");
                                }
                            }
                            else if (recipeReadStatus == RecipeReadStatus.Instruction)
                            {
                                // Assign value
                                recipes.Last().Add(line);
                            }
                            else
                            {
                                throw new FileFormatException("Could not parse file. No known sections found.");
                            }
                        }
                    } // Loop ends
                } // File closed

                recipes.Sort();

                // Set local class field reference to created recipe list.
                _recipes = recipes;

                // Notify that the file remains unmodified.
                IsModified = false;

                // Trigger event, let them know that we have read the file.
                OnRecipesChanged(EventArgs.Empty);
            }
            catch(Exception e)
            {
                // Write out error message
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }
            
        }

        public void Save()
        {
            try
            {
                // Open file, automaticly close it afterwards
                using (StreamWriter fileStream = new StreamWriter(@"..\..\App_Data\Recipes.txt"))
                {
                    foreach(IRecipe recipe in _recipes)
                    {
                        // Write Section Recipe
                        fileStream.WriteLine(SectionRecipe);

                        // Write Recipe name
                        fileStream.WriteLine(recipe.Name);

                        // Write Section Ingredients
                        fileStream.WriteLine(SectionIngredients);

                        foreach(Ingredient ingredient in recipe.Ingredients)
                        {
                            fileStream.WriteLine(String.Join(";", ingredient.Amount, ingredient.Measure, ingredient.Name));
                        }

                        // Write Section Instructions
                        fileStream.WriteLine(SectionInstructions);

                        foreach(string instruction in recipe.Instructions)
                        {
                            fileStream.WriteLine(instruction);
                        }
                    }

                    // Notify that the file is now modified.
                    IsModified = true;

                    // Trigger event, let them know that we have written to the file.
                    OnRecipesChanged(EventArgs.Empty);
                }
            }
            catch(Exception e)
            {
                // Write out error message
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }

        }
    }
}
