using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Transactions;

namespace SmallShopInventory
{
    public class Product
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int MinimumQuantity { get; set; }
        public int Quantity { get; set; }
    }

    public class JsonFileRepostiory
    {
        private readonly string _filepath;

        public JsonFileRepostiory (string filepath)
        {
            _filepath = filepath;

            string folderpath = Path.GetDirectoryName(_filepath);

            if (!Directory.Exists(folderpath))
                Directory.CreateDirectory(folderpath);

            if (!File.Exists(_filepath))
                File.WriteAllText(_filepath, "[]");
        }

        public List<Product> GetAll()
        {
            string ourJson = File.ReadAllText (_filepath);
            List<Product> allProductsList = new List<Product>();

            if(string.IsNullOrEmpty(ourJson)) return allProductsList;

            allProductsList = JsonSerializer.Deserialize<List<Product>>(ourJson);
            return allProductsList;
        } 

        public void Save(List<Product> product)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = JsonSerializer.Serialize(product, options);

            File.WriteAllText(_filepath, updatedJson);
        }
    }

    public class InventoryService
    {
        private readonly JsonFileRepostiory _jsonFileRepository;
        private List<Product> _productList;

        public InventoryService(JsonFileRepostiory jsonFileRepository)
        {
            _jsonFileRepository = jsonFileRepository;
            _productList = _jsonFileRepository.GetAll();
        }

        public List<Product> GetAll()
        {
            return _productList;
        }
        
        public void AddProduct(Product product)
        {
            _productList.Add(product);
            _jsonFileRepository.Save(_productList);
        }

        public bool CodeUniqueCheck(string code)
        {
            if(_productList.Any(c =>  c.Code == code))
                throw new InvalidOperationException($"Product with code '{code}' already exists.");
            return true;
        }

        public bool UpdateStock(string productCode, int quantityToChange)
        {
            var product = _productList.FirstOrDefault(p => p.Code.Equals(productCode));
            if (product != null)
            {
                if (quantityToChange < 0 && -quantityToChange > product.Quantity)
                    throw new ArgumentOutOfRangeException(nameof(quantityToChange), $"Cannot deduct {quantityToChange} items. Stock level is only {product.Quantity}."
        );
                product.Quantity += quantityToChange;
                _jsonFileRepository.Save(_productList);
                return true;
            }
            return false;
        }

    }

    class Program
    {
        static void Main()
        {
            string filepath = @"C:\Users\User\Desktop\AppData\products.json";
            JsonFileRepostiory jsonFileRepostiory = new JsonFileRepostiory(filepath);
            InventoryService inventoryService = new InventoryService(jsonFileRepostiory);
            bool t = true;
            while (t)
            {
                Console.WriteLine("-----Menu-----\n" +
                    "1. Add product\n" +
                    "2. View products\n" +
                    "3. Increase stock\n" +
                    "4. Decrease stock\n" +
                    "5. Sell product\n" +
                    "6. Show low-stock products\n" +
                    "7. Search product\n" +
                    "8. Export inventory report\n" +
                    "0. Exit\n");

                int operation;
                Console.Write("Write the number of your operation: ");
                while (!int.TryParse(Console.ReadLine(), out operation))
                {
                    Console.Write("Write the number from 0 to 8: ");
                }

                switch (operation)
                {
                    case 0:
                        t = false;
                        break;
                    case 1:
                        bool b = true;
                        while (b)
                        {
                            string code, name;
                            decimal price = 0;
                            int minimumQuantity = 0, quantity = 0;
                            try
                            {
                                Console.Write("Product Code: ");
                                code = Console.ReadLine();
                                if (inventoryService.CodeUniqueCheck(code))
                                {
                                    Console.Write("Product Name: ");
                                    name = Console.ReadLine();
                                    Console.Write("Product Price: ");
                                    while (!decimal.TryParse(Console.ReadLine(), out price) || price <= 0)
                                        Console.Write("Invalid input. Please enter a number greater than 0: ");

                                    Console.Write("Minimum Quantity: ");
                                    while (!int.TryParse(Console.ReadLine(), out minimumQuantity) || minimumQuantity <= 0)
                                        Console.Write("Invalid input. Please enter a number greater than 0: ");

                                    Console.Write("Quantity: ");
                                    while (!int.TryParse(Console.ReadLine(), out quantity) || quantity < 0)
                                        Console.Write("Invalid input. Please enter a positive number: ");

                                    Product product = new Product()
                                    {
                                        Code = code,
                                        Name = name,
                                        Price = price,
                                        MinimumQuantity = minimumQuantity,
                                        Quantity = quantity,
                                    };

                                    inventoryService.AddProduct(product);
                                    b = false;
                                }    
                            }
                            catch (InvalidOperationException ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");
                            }
                        }  
                        break;
                    case 2:
                        List<Product> products = inventoryService.GetAll();
                        int line = 1;
                        foreach (Product product in products) 
                        {
                            Console.WriteLine($"{line}. Code: {product.Code} | Name: {product.Name} | " +
                                $"Price: {product.Price} | Minimum Quantity: {product.MinimumQuantity} | " +
                                $"Quanitity: {product.Quantity}");
                            line++;
                        }
                        break;
                    case 3:
                        int changeRate = 0;
                        Console.Write("Product Code: ");
                        string neededProductCode = Console.ReadLine();

                        Console.Write("Quantity to add: ");
                        while (!int.TryParse(Console.ReadLine(), out changeRate) || changeRate < 0)
                            Console.Write("Invalid input. Write the quantity of growth: ");
                        if(!inventoryService.UpdateStock(neededProductCode, changeRate))
                            Console.WriteLine($"Product with code '{neededProductCode}' was not found.");
                        break;
                    case 4:
                        int change_Rate = 0;
                        Console.Write("Product Code: ");
                        string neededCode = Console.ReadLine();
                        
                        Console.Write("Quantity to reduce: ");
                        while (!int.TryParse(Console.ReadLine(), out change_Rate) || change_Rate < 0)
                            Console.Write("Invalid input. Write the quantity of reduce: ");
                        change_Rate = -change_Rate;
                        try
                        {
                            if (!inventoryService.UpdateStock(neededCode, change_Rate))
                                Console.WriteLine($"Product with code '{neededCode}' was not found.");
                        }
                        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
                        break;
                    case 5:
                        int quantitySold = 0;
                        Console.Write("Code of the product being sold: ");
                        string productCode = Console.ReadLine();
                        Console.Write("Quantity to sold: ");
                        while (!int.TryParse(Console.ReadLine(), out quantitySold) || quantitySold < 0)
                            Console.Write("Invalid input. Write the quantity: ");
                        quantitySold = -quantitySold;
                        try
                        {
                            if (!inventoryService.UpdateStock(productCode, quantitySold))
                                Console.WriteLine($"Product with code '{productCode}' was not found.");
                        }
                        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
                        break;
                    case 6:
                        var allProducts = inventoryService.GetAll();
                        var lowStock = allProducts.Where(h => h.Quantity <= h.MinimumQuantity).ToList();
                        int num = 1;

                        if (!lowStock.Any())
                        {
                            Console.WriteLine("All products are above minimum stock levels.");
                            break;
                        }

                        Console.WriteLine("---Low Stock Products---");
                        foreach(var product in lowStock)
                        {
                            Console.WriteLine($"{num}. Code: {product.Code} | Name: {product.Name} | " +
                                $"Price: {product.Price} | Minimum Quantity: {product.MinimumQuantity} | " +
                                $"Quanitity: {product.Quantity}");
                            num++;
                        }
                        break;
                    case 7:
                        Console.Write("Search (enter Product Code or Name): ");
                        string searchTerm = Console.ReadLine();

                        if (string.IsNullOrEmpty(searchTerm))
                        {
                            Console.WriteLine("Search term cannot be empty.");
                            break;
                        }

                        var searchResults = inventoryService.GetAll()
                            .Where(p => p.Code.Equals(searchTerm) ||
                                        p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (!searchResults.Any())
                        {
                            Console.WriteLine($"No products found matching '{searchTerm}'.");
                        }
                        else
                        {
                            Console.WriteLine($"\n--- Search Results ({searchResults.Count}) ---");
                            int searchLine = 1;
                            foreach (var product in searchResults)
                            {
                                Console.WriteLine($"{searchLine}. Code: {product.Code} | Name: {product.Name} | " +
                                $"Price: {product.Price} | Minimum Quantity: {product.MinimumQuantity} | " +
                                $"Quanitity: {product.Quantity}");
                                searchLine++;
                            }
                        }
                        break;
                    case 8:
                        var allProductsList = inventoryService.GetAll();

                        if (!allProductsList.Any())
                        {
                            Console.WriteLine("Inventory is empty. Cannot generate report.");
                            break;
                        }

                        int totalProductsCount = allProductsList.Count;
                        var lowStockProducts = allProductsList.Where(p => p.Quantity <= p.MinimumQuantity).ToList();
                        int lowStockCount = lowStockProducts.Count;
                        decimal totalInventoryValue = allProductsList.Sum(p => p.Price * p.Quantity);

                        System.Text.StringBuilder reportBuilder = new System.Text.StringBuilder();

                        reportBuilder.AppendLine("Inventory Report");
                        reportBuilder.AppendLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm}\n");
                        reportBuilder.AppendLine($"Total products: {totalProductsCount}");
                        reportBuilder.AppendLine($"Low stock products: {lowStockCount}");
                        reportBuilder.AppendLine($"Total inventory value: {totalInventoryValue} AMD\n");

                        reportBuilder.AppendLine("Low Stock:");
                        if (!lowStockProducts.Any())
                        {
                            reportBuilder.AppendLine("None");
                        }
                        else
                        {
                            int index = 1;
                            foreach (var p in lowStockProducts)
                            {
                                reportBuilder.AppendLine($"{index}. {p.Name} | Code: {p.Code} | Quantity: {p.Quantity} | Minimum: {p.MinimumQuantity}");
                                index++;
                            }
                        }

                        string finalReport = reportBuilder.ToString();

                        Console.WriteLine("\n" + finalReport);

                        string desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string reportFileName = $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmm}.txt";
                        string fullPath = Path.Combine(desktopDirectory, reportFileName);

                        try
                        {
                            File.WriteAllText(fullPath, finalReport);
                            Console.WriteLine($"Report successfully exported to Desktop:\n{fullPath}\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving report file: {ex.Message}");
                        }
                        break;
                }
            }
        }
    }
}       

