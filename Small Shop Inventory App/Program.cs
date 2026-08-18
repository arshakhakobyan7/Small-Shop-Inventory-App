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

            if (!string.IsNullOrEmpty(folderpath) && !Directory.Exists(folderpath))
                Directory.CreateDirectory(folderpath);

            if (!File.Exists(_filepath))
                File.WriteAllText(_filepath, "[]");
        }

        public List<Product> GetAll()
        {
            try
            {
                string ourJson = File.ReadAllText(_filepath);
                List<Product> allProductsList = new List<Product>();

                if (string.IsNullOrEmpty(ourJson)) return allProductsList;

                allProductsList = JsonSerializer.Deserialize<List<Product>>(ourJson);
                return allProductsList;
            }
            catch (JsonException ex)
            {
                throw new DataStorageException($"Failed to load products: The storage file at '{_filepath}' contains invalid or corrupted JSON.", ex);
            }
            catch (IOException ex) 
            {
                throw new DataStorageException($"Failed to read storage file at '{_filepath}'.", ex);
            }
        } 

        public void Save(List<Product> product)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string updatedJson = JsonSerializer.Serialize(product, options);

                File.WriteAllText(_filepath, updatedJson);
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException)
            {
                throw new DataStorageException($"Failed to save products to storage file at '{_filepath}'.", ex);
            }
        }
    }

    public class DataStorageException : Exception
    {
        public DataStorageException(string message, Exception innerException)
            : base(message, innerException)
        {
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

        public IReadOnlyList<Product> GetAll()
        {
            return _productList.AsReadOnly();
        }

        public bool AddProduct(string code, string name, string price, string minimumquantity, string quantity)
        {
            if(string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || 
                string.IsNullOrWhiteSpace(price) || string.IsNullOrWhiteSpace(minimumquantity) ||
                string.IsNullOrWhiteSpace(quantity))
                return false;
            
            if (!CodeUniqueCheck(code)) 
                return false;
            
            if(decimal.TryParse(price, out var pricee) && int.TryParse(minimumquantity, out var minimumquantityy) && int.TryParse(quantity, out var quantityy))
            {
                if(pricee > 0 && quantityy >= 0 && minimumquantityy >= 0)
                {
                    Product product = new Product
                    {
                        Code = code.Trim(),
                        Name = name.Trim(),
                        Price = pricee,
                        MinimumQuantity = minimumquantityy,
                        Quantity = quantityy
                    };
                    _productList.Add(product);
                    _jsonFileRepository.Save(_productList);
                    return true;
                }  
            }
            return false;
        }

        public bool CodeUniqueCheck(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) 
                return false;

            if (_productList.Any(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }

        public bool SellProduct(string productCode, string quantityToChange)
        {
            var product = _productList.FirstOrDefault(p => p.Code.Equals(productCode, StringComparison.OrdinalIgnoreCase));

            if (product == null)
                return false;

            if(!int.TryParse(quantityToChange, out var quantitySold) || quantitySold <= 0 || quantitySold > product.Quantity)
                return false;
            
            product.Quantity -= quantitySold;
            _jsonFileRepository.Save(_productList);
            return true;
        }

        public bool IncreaseStock(string productCode, int quantityToIncrease)
        {
            if (quantityToIncrease <= 0) return false;

            var product = _productList.FirstOrDefault(p => p.Code.Equals(productCode, StringComparison.OrdinalIgnoreCase));

            if (product == null)
                return false;

            product.Quantity += quantityToIncrease;
            _jsonFileRepository.Save(_productList);
            return true;
        }

        public bool DecreaseStock(string productCode, int quantityToDecrease)
        {
            if (quantityToDecrease <= 0) return false;

            var product = _productList.FirstOrDefault(p => p.Code.Equals( productCode, StringComparison.OrdinalIgnoreCase));

            if (product == null)
                return false;

            if (quantityToDecrease > product.Quantity)
                return false;

            product.Quantity -= quantityToDecrease;
            _jsonFileRepository.Save(_productList);
            return true;
        }

    }

    class Program
    {
        static void Main()
        {
            string filepath = @"C:\Users\User\Desktop\AppData\products.json";
            JsonFileRepostiory jsonFileRepostiory;
            InventoryService inventoryService;

            static string ReadInput() => Console.ReadLine() ?? string.Empty;

            try
            {
                jsonFileRepostiory = new JsonFileRepostiory(filepath);
                inventoryService = new InventoryService(jsonFileRepostiory);
            }
            catch (DataStorageException ex)
            {
                Console.WriteLine($"[CRITICAL ERROR] Could not start application.");
                Console.WriteLine($"Details: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Root cause: {ex.InnerException.Message}");
                }
                Console.WriteLine("\nPlease fix or delete the corrupted JSON file and restart.");
                return;
            }

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
                while (!int.TryParse(ReadInput(), out operation))
                {
                    Console.Write("Write the number from 0 to 8: ");
                }

                switch (operation)
                {
                    case 0:
                        t = false;
                        break;
                    case 1:
                        Console.WriteLine("If you want to add a product, you will be asked for the " +
                            "product code, name, price, minimum quantity, and quantity. " +
                            "None of the fields can be empty, the code must be unique, " +
                            "the price must be greater than 0, the minimum quantity must be positive, and the quantity must be non-negative.");

                        bool b = true;
                        while (b)
                        {
                            Console.Write("Product Code: ");
                            string code = ReadInput();

                            Console.Write("Product Name: ");
                            string name = ReadInput();

                            Console.Write("Product Price: ");
                            string price = ReadInput();

                            Console.Write("Minimum Quantity: ");
                            string minimumQuantity = ReadInput();

                            Console.Write("Quantity: ");
                            string quantity = ReadInput();

                            if (!inventoryService.AddProduct(code, name, price, minimumQuantity, quantity))
                            {
                                Console.WriteLine("Follow the requirements above.");
                                Console.Write("Try again?(Yes - 0): ");
                                string answer = ReadInput();
                                if (answer != "0")
                                    b = false;
                            }
                            else
                                b = false;
                        }
                        break;
                    case 2:
                        var products = inventoryService.GetAll();
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
                        string neededProductCode = ReadInput();

                        Console.Write("Quantity to add: ");
                        while (!int.TryParse(ReadInput(), out changeRate) || changeRate <= 0)
                            Console.Write("Invalid input. Write the quantity of growth: ");
                        if (!inventoryService.IncreaseStock(neededProductCode, changeRate))
                            Console.WriteLine("Try again. No product found with this code.");
                        break;
                    case 4:
                        int change_Rate = 0;
                        Console.Write("Product Code: ");
                        string neededCode = ReadInput();
                        
                        Console.Write("Quantity to reduce: ");
                        while (!int.TryParse(ReadInput(), out change_Rate) || change_Rate <= 0)
                            Console.Write("Invalid input. Write the quantity of reduce: ");
                        if(!inventoryService.DecreaseStock(neededCode, change_Rate))
                            Console.WriteLine("Try again. Make sure that the product with such code is available or in sufficient quantity.");
                        break;
                    case 5:
                        Console.Write("Code of the product being sold: ");
                        string productCode = ReadInput();

                        Console.Write("Quantity to sold: ");
                        string qunatityToSold = ReadInput();

                        if (!inventoryService.SellProduct(productCode, qunatityToSold))
                            Console.WriteLine("This transaction cannot be completed.");

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
                        string searchTerm = ReadInput();

                        if (string.IsNullOrEmpty(searchTerm))
                        {
                            Console.WriteLine("Search term cannot be empty.");
                            break;
                        }

                        var searchResults = inventoryService.GetAll()
                            .Where(p => p.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
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

