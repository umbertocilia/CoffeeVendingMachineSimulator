using CoffeeMachine.Domain;
using MachineState = CoffeeMachine.Domain.CoffeeMachine;

namespace CoffeeMachine.Infrastructure.Persistence;

public static class MachineSeedFactory
{
    public static MachineState Create(DateTimeOffset now) => new()
    {
        Id = "office-coffee-machine",
        Status = MachineStatus.Off,
        PowerOn = false,
        Boiler = new Boiler { CurrentTemperature = 22, TargetTemperature = 90 },
        WaterTank = new WaterTank { CapacityMl = 5000, CurrentLevelMl = 4500, LowLevelThresholdMl = 750 },
        Ingredients =
        [
            new IngredientContainer { Id = "coffee-beans", Name = "Coffee", Unit = "g", Capacity = 3000, CurrentLevel = 2500, LowLevelThreshold = 350 },
            new IngredientContainer { Id = "milk-powder", Name = "Milk", Unit = "g", Capacity = 2000, CurrentLevel = 1600, LowLevelThreshold = 250 },
            new IngredientContainer { Id = "chocolate", Name = "Chocolate", Unit = "g", Capacity = 2000, CurrentLevel = 1500, LowLevelThreshold = 250 },
            new IngredientContainer { Id = "sugar", Name = "Sugar", Unit = "g", Capacity = 2000, CurrentLevel = 1800, LowLevelThreshold = 200 }
        ],
        Products =
        [
            new Product { Id = "espresso", Name = "Espresso", RecipeId = "espresso-recipe", Price = 0.5m, Enabled = true },
            new Product { Id = "cappuccino", Name = "Cappuccino", RecipeId = "cappuccino-recipe", Price = 0.8m, Enabled = true },
            new Product { Id = "hot-chocolate", Name = "Hot Chocolate", RecipeId = "hot-chocolate-recipe", Price = 1.2m, Enabled = true }
        ],
        Recipes =
        [
            new Recipe
            {
                Id = "espresso-recipe",
                Name = "Espresso Recipe",
                TargetTemperature = 91,
                Steps =
                [
                    new RecipeStep { Sequence = 1, IngredientKey = "water", Quantity = 40, Unit = "ml", DurationMs = 1800 },
                    new RecipeStep { Sequence = 2, IngredientKey = "Coffee", Quantity = 9, Unit = "g", DurationMs = 1000 }
                ]
            },
            new Recipe
            {
                Id = "cappuccino-recipe",
                Name = "Cappuccino Recipe",
                TargetTemperature = 93,
                Steps =
                [
                    new RecipeStep { Sequence = 1, IngredientKey = "water", Quantity = 50, Unit = "ml", DurationMs = 1800 },
                    new RecipeStep { Sequence = 2, IngredientKey = "Coffee", Quantity = 9, Unit = "g", DurationMs = 1000 },
                    new RecipeStep { Sequence = 3, IngredientKey = "Milk", Quantity = 15, Unit = "g", DurationMs = 1400 }
                ]
            },
            new Recipe
            {
                Id = "hot-chocolate-recipe",
                Name = "Hot Chocolate Recipe",
                TargetTemperature = 88,
                Steps =
                [
                    new RecipeStep { Sequence = 1, IngredientKey = "water", Quantity = 100, Unit = "ml", DurationMs = 2000 },
                    new RecipeStep { Sequence = 2, IngredientKey = "Chocolate", Quantity = 18, Unit = "g", DurationMs = 1700 },
                    new RecipeStep { Sequence = 3, IngredientKey = "Sugar", Quantity = 4, Unit = "g", DurationMs = 800 }
                ]
            }
        ],
        RecentEvents =
        [
            new EventLogEntry { TimestampUtc = now, Category = "bootstrap", Message = "Initial machine seed created." }
        ],
        Sensors = new SensorState { WaterLevelPercentage = 90, BoilerTemperature = 22, AverageIngredientPercentage = 83 }
    };
}
