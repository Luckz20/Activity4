using System.Data.OleDb;
using System.Security.Cryptography;
using System.Text;

namespace form1
{
    internal sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    internal sealed class UserRecord
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime? DateCreated { get; set; }
    }

    internal static class Db
    {
        public static string ConnectionString
        {
            get
            {
                string dbPath = Path.Combine(
                    Application.StartupPath,
                    "InventoryDB.accdb"
                );

                if (!File.Exists(dbPath))
                {
                    throw new FileNotFoundException(
                        "InventoryDB.accdb not found: " + dbPath
                    );
                }

                return @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                       dbPath + ";Persist Security Info=False;";
            }
        }

        public static void EnsureProductsTable()
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            bool exists;
            try
            {
                using var check = new OleDbCommand(
                    "SELECT TOP 1 [ProductID] FROM [Products]", con);
                check.ExecuteScalar();
                exists = true;
            }
            catch (OleDbException)
            {
                exists = false;
            }

            if (exists) return;

            using (var create = new OleDbCommand(
                "CREATE TABLE [Products] (" +
                "[ProductID] AUTOINCREMENT PRIMARY KEY, " +
                "[ProductName] TEXT(100) NOT NULL, " +
                "[Category] TEXT(50), " +
                "[Quantity] LONG NOT NULL, " +
                "[Price] CURRENCY NOT NULL, " +
                "[DateAdded] DATETIME)", con))
            {
                create.ExecuteNonQuery();
            }

            var seed = new[]
            {
                new Product { Name = "Rice 5kg",       Category = "Food",          Quantity = 50,  Price = 250 },
                new Product { Name = "Cooking Oil 1L", Category = "Food",          Quantity = 30,  Price = 90  },
                new Product { Name = "Sardines Can",   Category = "Canned Goods",  Quantity = 100, Price = 25  },
                new Product { Name = "Shampoo 400ml",  Category = "Personal Care", Quantity = 20,  Price = 120 },
                new Product { Name = "Laundry Soap",   Category = "Household",     Quantity = 5,   Price = 45  },
                new Product { Name = "Biscuit",        Category = "Snacks",        Quantity = 0,   Price = 35  },
            };

            foreach (var p in seed)
            {
                InsertProduct(con, p);
            }
        }

        public static List<Product> GetProducts()
        {
            var list = new List<Product>();

            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using var cmd = new OleDbCommand(
                "SELECT [ProductID], [ProductName], [Category], [Quantity], [Price] " +
                "FROM [Products] ORDER BY [ProductID]", con);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Product
                {
                    Id = Convert.ToInt32(r["ProductID"]),
                    Name = r["ProductName"] is DBNull ? "" : Convert.ToString(r["ProductName"]) ?? "",
                    Category = r["Category"] is DBNull ? "" : Convert.ToString(r["Category"]) ?? "",
                    Quantity = r["Quantity"] is DBNull ? 0 : Convert.ToInt32(r["Quantity"]),
                    Price = r["Price"] is DBNull ? 0m : Convert.ToDecimal(r["Price"]),
                });
            }

            return list;
        }

        public static int AddProduct(Product p)
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            InsertProduct(con, p);

            using var idCmd = new OleDbCommand("SELECT @@IDENTITY", con);
            return Convert.ToInt32(idCmd.ExecuteScalar());
        }

        public static void UpdateProduct(Product p)
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using var cmd = new OleDbCommand(
                "UPDATE [Products] SET [ProductName] = ?, [Category] = ?, " +
                "[Quantity] = ?, [Price] = ? WHERE [ProductID] = ?", con);

            cmd.Parameters.Add("@ProductName", OleDbType.VarWChar).Value = p.Name;
            cmd.Parameters.Add("@Category", OleDbType.VarWChar).Value = p.Category;
            cmd.Parameters.Add("@Quantity", OleDbType.Integer).Value = p.Quantity;
            cmd.Parameters.Add("@Price", OleDbType.Currency).Value = p.Price;
            cmd.Parameters.Add("@ProductID", OleDbType.Integer).Value = p.Id;

            cmd.ExecuteNonQuery();
        }

        public static void DeleteProduct(int id)
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using var cmd = new OleDbCommand(
                "DELETE FROM [Products] WHERE [ProductID] = ?", con);

            cmd.Parameters.Add("@ProductID", OleDbType.Integer).Value = id;
            cmd.ExecuteNonQuery();
        }

        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static List<UserRecord> GetUsers()
        {
            var list = new List<UserRecord>();

            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using var cmd = new OleDbCommand(
                "SELECT [UserID], [FullName], [Username], [Email], [DateCreated] " +
                "FROM [Users] ORDER BY [UserID]", con);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new UserRecord
                {
                    Id = Convert.ToInt32(r["UserID"]),
                    FullName = r["FullName"] is DBNull ? "" : Convert.ToString(r["FullName"]) ?? "",
                    Username = r["Username"] is DBNull ? "" : Convert.ToString(r["Username"]) ?? "",
                    Email = r["Email"] is DBNull ? "" : Convert.ToString(r["Email"]) ?? "",
                    DateCreated = r["DateCreated"] is DBNull ? null : Convert.ToDateTime(r["DateCreated"]),
                });
            }

            return list;
        }

        public static bool UserExists(string username, string email, int excludeId)
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using var cmd = new OleDbCommand(
                "SELECT COUNT(*) FROM [Users] " +
                "WHERE ([Username] = ? OR [Email] = ?) AND [UserID] <> ?", con);

            cmd.Parameters.Add("@Username", OleDbType.VarWChar).Value = username;
            cmd.Parameters.Add("@Email", OleDbType.VarWChar).Value = email;
            cmd.Parameters.Add("@UserID", OleDbType.Integer).Value = excludeId;

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static int CountUsers()
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using var cmd = new OleDbCommand("SELECT COUNT(*) FROM [Users]", con);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static int AddUser(UserRecord u, string plainPassword)
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using (var cmd = new OleDbCommand(
                "INSERT INTO [Users] " +
                "([FullName], [Username], [Email], [UserPassword], [DateCreated]) " +
                "VALUES (?, ?, ?, ?, ?)", con))
            {
                cmd.Parameters.Add("@FullName", OleDbType.VarWChar).Value = u.FullName;
                cmd.Parameters.Add("@Username", OleDbType.VarWChar).Value = u.Username;
                cmd.Parameters.Add("@Email", OleDbType.VarWChar).Value = u.Email;
                cmd.Parameters.Add("@UserPassword", OleDbType.VarWChar, 64).Value = HashPassword(plainPassword);
                cmd.Parameters.Add("@DateCreated", OleDbType.Date).Value = DateTime.Now;
                cmd.ExecuteNonQuery();
            }

            using var idCmd = new OleDbCommand("SELECT @@IDENTITY", con);
            return Convert.ToInt32(idCmd.ExecuteScalar());
        }

        public static void UpdateUser(UserRecord u, string? newPassword)
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            bool changePw = !string.IsNullOrEmpty(newPassword);

            using var cmd = new OleDbCommand(
                changePw
                    ? "UPDATE [Users] SET [FullName] = ?, [Username] = ?, [Email] = ?, [UserPassword] = ? WHERE [UserID] = ?"
                    : "UPDATE [Users] SET [FullName] = ?, [Username] = ?, [Email] = ? WHERE [UserID] = ?",
                con);

            cmd.Parameters.Add("@FullName", OleDbType.VarWChar).Value = u.FullName;
            cmd.Parameters.Add("@Username", OleDbType.VarWChar).Value = u.Username;
            cmd.Parameters.Add("@Email", OleDbType.VarWChar).Value = u.Email;
            if (changePw)
                cmd.Parameters.Add("@UserPassword", OleDbType.VarWChar, 64).Value = HashPassword(newPassword!);
            cmd.Parameters.Add("@UserID", OleDbType.Integer).Value = u.Id;

            cmd.ExecuteNonQuery();
        }

        public static void DeleteUser(int id)
        {
            using var con = new OleDbConnection(ConnectionString);
            con.Open();

            using var cmd = new OleDbCommand(
                "DELETE FROM [Users] WHERE [UserID] = ?", con);

            cmd.Parameters.Add("@UserID", OleDbType.Integer).Value = id;
            cmd.ExecuteNonQuery();
        }

        private static void InsertProduct(OleDbConnection con, Product p)
        {
            using var cmd = new OleDbCommand(
                "INSERT INTO [Products] " +
                "([ProductName], [Category], [Quantity], [Price], [DateAdded]) " +
                "VALUES (?, ?, ?, ?, ?)", con);

            cmd.Parameters.Add("@ProductName", OleDbType.VarWChar).Value = p.Name;
            cmd.Parameters.Add("@Category", OleDbType.VarWChar).Value = p.Category;
            cmd.Parameters.Add("@Quantity", OleDbType.Integer).Value = p.Quantity;
            cmd.Parameters.Add("@Price", OleDbType.Currency).Value = p.Price;
            cmd.Parameters.Add("@DateAdded", OleDbType.Date).Value = DateTime.Now;

            cmd.ExecuteNonQuery();
        }
    }
}
