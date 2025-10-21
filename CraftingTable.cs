using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("Crafting section")]
    public List<Item> dish = new List<Item>();

    public List<Item> food = new List<Item>();

    private int NumberOfInstantNoodle = 0;

    private int NumberOfRice = 0;

    private int NumberOfBread = 0;

    private int NumberOfLunchMeat = 0;

    private int NumberOfSausage = 0;

    private int NumberOfWater = 0;

    private int NumberOfDishes = 0;

    private int NumebrOfFruitJam = 0;

    public void AddToFoodList(Item item)
    {
        Item tmpItem = item;
        food.Add (tmpItem);
    }

    public void PrepareToCook(Item item)
    {
        //add the food item in the cooker
        Item tmpItem = item;
        food.Add (tmpItem);
    }

    public void CancelCook(Item item)
    {
        //remove the food item in the cooker
        food.Remove (item);
    }

    public void StartToCook()
    {
        //reset the number of dish and food to 0
        NumberOfDishes = 0;
        NumberOfInstantNoodle = 0;
        NumberOfRice = 0;
        NumberOfBread = 0;
        NumberOfLunchMeat = 0;
        NumberOfSausage = 0;
        NumberOfWater = 0;
        NumberOfDishes = 0;
        NumebrOfFruitJam = 0;

        //calculate number of food element in the food list
        for (int i = 0; i < food.Count; i++)
        {
            switch (food[i].ItemName)
            {
                case "InstantNoodle":
                    NumberOfInstantNoodle += 1;
                    break;
                case "Rice":
                    NumberOfRice += 1;
                    break;
                case "Bread":
                    NumberOfBread += 1;
                    break;
                case "LunchMeat":
                    NumberOfLunchMeat += 1;
                    break;
                case "Sausage":
                    NumberOfSausage += 1;
                    break;
                case "Water":
                    NumberOfWater += 1;
                    break;
                case "FruitJam":
                    NumebrOfFruitJam += 1;
                    break;
                default:
                    Debug
                        .Log("The Item Name is not assigned or the item type is wrong");
                    break;
            }
        }

        //Instant noodle recipe
        if (
            NumberOfInstantNoodle != 0 &&
            NumberOfRice == 0 &&
            NumberOfBread == 0 &&
            NumberOfWater != 0
        )
        {
            if (NumberOfInstantNoodle == 1 && NumberOfWater == 1)
            {
                //remove all food items in the cooker
                food.Clear();

                //add fried instant noodle UI in cooker
                AddToFoodList(dish[0]);

                //spawn Fried instant noodle
                Debug.Log("The fried noodle has been cooked");
            }
            else if (NumberOfInstantNoodle == 1 && NumberOfWater == 2)
            {
                //spawn instant noodle with soup
                Debug.Log("The instant noodle with soup has been cooked");
            }
            else
            {
                //cannot cook the item
                Debug.Log("The food cannot be cooked");
            }
        } //Rice recipe
        else if (
            NumberOfRice != 0 &&
            NumberOfInstantNoodle == 0 &&
            NumberOfBread == 0 &&
            NumberOfWater != 0
        )
        {
            NumberOfDishes = NumberOfRice;
            switch (NumberOfRice)
            {
                case 1:
                    // Implement one-rice dish logic
                    break;
                case 2:
                    // Implement two-rice dish logic
                    break;
                case 3:
                    // Implement three-rice dish logic
                    break;
            }
        } //Bread recipe
        else if (
            NumberOfBread != 0 &&
            NumberOfRice == 0 &&
            NumberOfInstantNoodle == 0
        )
        {
            NumberOfDishes = NumberOfBread;
            switch (NumberOfBread)
            {
                case 1:
                    // Implement one-bread dish logic
                    break;
                case 2:
                    // Implement two-bread dish logic
                    break;
                case 3:
                    // Implement three-bread dish logic
                    break;
            }
        }
        else
        {
            //cannot cook the item
            Debug.Log("The item cannot be crafted");
        }
    }
}
