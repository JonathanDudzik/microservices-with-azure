using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using RedDog.VirtualCustomer.Models;

namespace RedDog.VirtualCustomer
{
    public class CreateOrders
    {
        private static readonly string orderServiceUrl = Environment.GetEnvironmentVariable("ORDER_SERVICE_URL");
        private static readonly string storeId = Environment.GetEnvironmentVariable("STORE_ID") ?? "Redmond";
        public static readonly int maxItemQuantity = int.Parse(Environment.GetEnvironmentVariable("MAX_ITEM_QUANTITY") ?? "1");
        public static readonly int maxUniqueItemsPerOrder = int.Parse(Environment.GetEnvironmentVariable("MAX_UNIQUE_ITEMS_PER_ORDER") ?? "10");
        public static readonly int minSecondsToPlaceOrder = int.Parse(Environment.GetEnvironmentVariable("MIN_SEC_TO_PLACE_ORDER") ?? "1");
        public static readonly int maxSecondsToPlaceOrder = int.Parse(Environment.GetEnvironmentVariable("MAX_SEC_TO_PLACE_ORDER") ?? "3");
        public static readonly int minSecondsBetweenOrders = int.Parse(Environment.GetEnvironmentVariable("MIN_SEC_BETWEEN_ORDERS") ?? "1");
        public static readonly int maxSecondsBetweenOrders = int.Parse(Environment.GetEnvironmentVariable("MAX_SEC_BETWEEN_ORDERS") ?? "3");
        public static readonly int numOrders = int.Parse(Environment.GetEnvironmentVariable("NUM_ORDERS") ?? "-1");
        private readonly HttpClient _httpClient;
        private List<Product> _products;
        private Random _random;

        private static readonly (int, string, string)[] customers = {
            (1,"Bruce","Wayne"),
            (2,"Lou","Redwood"),
            (3,"Allan","Austin"),
            (4,"Rusty","Ryan"),
            (5,"Aubrey","Curtis"),
            (6,"Aldo","Raine"),
            (7,"Magrit","Reck"),
            (8,"Mason","Lucas"),
            (9,"Etienne","Tremblay"),
            (10,"Myrtle","Martin"),
            (11,"Adelgunde","Rode"),
            (12,"Matt","Wallace"),
            (13,"Dick","Pepperfield"),
            (14,"Beatrice","Gagnon"),
            (15,"Julian","Harris"),
            (16,"Ellinor","Flick"),
            (17,"Nils","Rüdiger"),
            (18,"Bea","Arthur"),
            (19,"Mickey","O'Neil"),
            (20,"Cindy","Powell"),
            (21,"Brielle","Singh"),
            (22,"Jackie","Moon"),
            (23,"Ilhan","Kaps"),
            (24,"Arthur","Grewal"),
            (25,"Scarlett","Hawkins"),
            (26,"Brennan","Huff"),
            (27,"Marilou","Williams"),
            (28,"Estelle","Getty"),
            (29,"Camille","Macdonald"),
            (30,"Danielle","Niemeier"),
            (31,"Tom","Bishop"),
            (32,"Dale","Doback"),
            (33,"Logan","Scott"),
            (34,"Darlene","Gregory"),
            (35,"Cal","Naughton"),
            (36,"Megan","Singh"),
            (37,"Jeff","Goldblum"),
            (38,"Cody","Chapman"),
            (39,"Christina","Snyder"),
            (40,"Rafi","Cunado"),
            (41,"Leah","Holmes"),
            (42,"Brielle","Harcourt"),
            (43,"Amelia","Edwards"),
            (44,"Leon","Green"),
            (45,"Elizabeth","Arnold"),
            (46,"Ricky","Bobby"),
            (47,"Hardy","Backhaus"),
            (48,"Renato","Jahnke"),
            (49,"Zoe","Bélanger"),
            (50,"Taco","MacArthur"),
            (51,"Kelly","Harrison"),
            (52,"Betty","White"),
            (53,"Allen","Gamble"),
            (54,"Alex","Clarke"),
            (55,"Sandra","Bailey"),
            (56,"Jacqueline","White"),
            (57,"Erik","Lehnsherr"),
            (58,"Joyce","Roberts"),
            (59,"Lucille","Gutierrez"),
            (60,"Ashley","Mills"),
            (61,"Antoine","Thompson"),
            (62,"Maddison","Hall"),
            (63,"Franz","Rütten"),
            (64,"Hildegund","Esch"),
            (65,"Lorraine","Silva"),
            (66,"Steffi","Graf"),
            (67,"Amelia","Hill"),
            (68,"Sophia","Williams"),
            (69,"Patricia","Little"),
            (70,"Peter","Weyland"),
            (71,"Wanda","Simpson"),
            (72,"Tyler","Durden"),
            (73,"Dorothy","Mantooth"),
            (74,"Sloan","Sabbath"),
            (75,"Roy","Hobbs"),
            (76,"Jerry","Rice"),
            (77,"Christa","Ebert"),
            (78,"Lily","Grewal"),
            (79,"Benjamin","Perkins"),
            (80,"Debra","Smith"),
            (81,"Elliot","Park"),
            (82,"Aubree","Williamson"),
            (83,"Madison","Sanchez"),
            (84,"Terry","Hoitz"),
            (85,"Julie","Chavez"),
            (86,"Russ","Hanneman"),
            (87,"Daniel","Gagné"),
            (88,"Bertram","Gilfoyle"),
            (89,"Emma","Garcia"),
            (90,"Leo","Spaceman"),
            (91,"Clarence","Gilbert"),
            (92,"Jessie","Ray"),
            (93,"Kenny","Powers"),
            (94,"Pippa","Wang"),
            (95,"Jack","Barker"),
            (96,"Jean-Baptiste","Zorg"),
            (97,"Joe","Montana"),
            (98,"Cathy","Perry"),
            (99,"Valerie","Watkins"),
            (100,"Abigail","Andersen"),
            (101,"Megan","Barnaby"),
            (102,"Erin","Rhodes"),
            (103,"Lane","Myer"),
            (104,"Charlie","Lo"),
            (105,"Monique","Junot"),
            (106,"Calvin","Joyner"),
            (107,"Walter","White"),
            (108,"Mia","Brar"),
            (109,"MacKenzie","McHale"),
            (110,"Ty","Webb"),
            (111,"Allison","Bryant"),
            (112,"Jesse","Pinkman"),
            (113,"Natalie","Thompson"),
            (114,"Ashley","Carlson"),
            (115,"Gustavo","Fring"),
            (116,"Danny","Noonan"),
            (117,"Leah","Thompson"),
            (118,"Elizabeth","Mitchell"),
            (119,"John","McClane"),
            (120,"Archer","Smith"),
            (121,"Vincent","Hanna"),
            (122,"Jon","Garrett"),
            (123,"Dwight","Schrute"),
            (124,"Vicki","Scott"),
            (125,"Dwayne","Holt"),
            (126,"Fletcher","Clarke"),
            (127,"Grace","Cox"),
            (128,"Mika","Haubold"),
            (129,"Joe","Kent"),
            (130,"Alexandra","Roberts"),
            (131,"Will","McAvoy"),
            (132,"Leo","Knight"),
            (133,"Maeva","Claire"),
            (134,"Keira","Martin"),
            (135,"Amanda","Washington"),
            (136,"Hans","Gruber"),
            (137,"Judy","Gomez"),
            (138,"Sergio","Ebeling"),
            (139,"Quinn","Moore"),
            (140,"Michael","Bolton"),
            (141,"Eliza","Grant"),
            (142,"Jill","Grant"),
            (143,"Zoltan","Haller"),
            (144,"Ramon","Ross"),
            (145,"Carl","Spackler"),
            (146,"Addison","Park"),
            (147,"Walter","Sobchak"),
            (148,"Mia","Mackay"),
            (149,"Korben","Dallas"),
            (150,"Milton","Waddams"),
            (151,"Brent","Rodriguez"),
            (152,"Melodie","Pelletier"),
            (153,"Emily","Jackson"),
            (154,"Neil","McCauley"),
            (155,"Jessica","Lawrence"),
            (156,"Julia","Lam"),
            (157,"Sibilla","Knappe"),
            (158,"Al","Czervik"),
            (159,"Harry","Ellis"),
            (160,"Emma","Williams"),
            (161,"Kristen","Arnold"),
            (162,"Heywood","Floyd"),
            (163,"Hannah","Mackay"),
            (164,"Larry","Sellers"),
            (165,"Elihu","Smails"),
            (166,"Cole","Trickle"),
            (167,"Rita","Vrataski"),
            (168,"Peter","Gibbons"),
            (169,"Kayla","Smith"),
            (170,"Theo","Morris"),
            (171,"Edith","Carpenter"),
            (172,"Sherlock","Holmes"),
            (173,"David","Bowman"),
            (174,"Clark","Griswold"),
            (175,"Marilou","Park"),
            (176,"Phil","Wenneck"),
            (177,"Milan","Scholze"),
            (178,"Irwin","Fletcher"),
            (179,"Irene","Foster"),
            (180,"Leeloo","Dallas"),
            (181,"Jack","Harper"),
            (182,"Jill","Lambert"),
            (183,"Linda","Hunter"),
            (184,"Maritta","Walch"),
            (185,"Harris","Telemacher"),
            (186,"Phoebe","Rice"),
            (187,"Estelle","Rohrer"),
            (188,"Malcolm","Beech"),
            (189,"Bill","Lumberg"),
            (190,"Chris","Kyle"),
            (191,"Miranda","Priestly"),
            (192,"Andrea","Sachs"),
            (193,"Kirk","Lazarus"),
            (194,"Katniss","Everdeen"),
            (195,"Vinnie","Barbarino"),
            (196,"Mia","Hill"),
            (197,"Les","Grossman"),
            (198,"Jeffrey","Lebowski"),
            (199,"Wilma","Ramos"),
            (200,"Rachel","Alvarez")
        };

        public CreateOrders(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(orderServiceUrl);
            _random = new Random();
        }

        [FunctionName("CreateOrders")]
        public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer, ILogger log, CancellationToken stoppingToken)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            int ordersCreated = 0;

            try
            {
                log.LogInformation($"Retrieving products from: {_httpClient.BaseAddress}/product");
                _products = await _httpClient.GetAsync<List<Product>>("product");
                log.LogInformation($"Successfully retrieved {_products.Count} products");

                await CreateOrder(log, stoppingToken);
                ordersCreated += 1;
            }
            catch (Exception e)
            {
                log.LogError("Error retrieving products. Retrying in 5 seconds. Message: {Message}", e.InnerException?.Message ?? e.Message);
            }
        }

        public async Task CreateOrder(ILogger log, CancellationToken stoppingToken)
        {
            CustomerOrder order = new CustomerOrder();
            int custNum = _random.Next(0, customers.Length);
            log.LogInformation("Customer {0} {1} with loyalty id {2} is placing an order.", customers[custNum].Item2, customers[custNum].Item3, customers[custNum].Item1);
            order.StoreId = storeId;
            order.FirstName = customers[custNum].Item2;
            order.LastName = customers[custNum].Item3;
            order.LoyaltyId = customers[custNum].Item1.ToString();

            // Get total number of menu items (t) and ramdomly choose a number of them to order (n)
            int numProducts = _products.Count;
            int numOrderItems = _random.Next(1, Math.Min(numProducts, maxUniqueItemsPerOrder)); // Never order 0 items

            // Randomly choose a number between (1-t), (n) times to get a random quantity of (n) items
            List<CustomerOrderItem> orderItems = new List<CustomerOrderItem>();
            List<int> productIndicies = new List<int>();
            for (int i = 0; i < numOrderItems; i++)
            {
                int productIndex = _random.Next(1, numProducts + 1);
                while (productIndicies.Contains(productIndex)) // Ensure no duplicate menu items within order
                {
                    productIndex = _random.Next(1, numProducts + 1);
                }

                productIndicies.Add(productIndex);
                var menuItem = _products.Find(x => x.ProductId == productIndex); // Assuming menuIds are numbered #1 to #menuItemTotal
                int quantity = _random.Next(1, maxItemQuantity + 1);

                orderItems.Add(new CustomerOrderItem
                {
                    ProductId = productIndex,
                    Quantity = quantity
                });
            }

            order.OrderItems = orderItems;
            if (!stoppingToken.IsCancellationRequested)
            {
                log.LogInformation($"Placing orders at: {_httpClient.BaseAddress}/order");
                var response = await _httpClient.PostAsync("order", new JsonContent(order)).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    log.LogInformation("Order was unsuccessful: {0} {1} {2}", (int)response.StatusCode, response.StatusCode, await response.Content.ReadAsStringAsync());
                }
                else
                {
                    log.LogInformation("Customer {0} {1} has placed their order containing {2} items.", order.FirstName, order.LastName, order.OrderItems.Count);
                }
            }
        }
    }
}
