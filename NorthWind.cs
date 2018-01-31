using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.ClientConventions;
using FindConsole.NorthwindEntities;
using FindForDevelopers;
using System;
using System.Data.Entity;
using System.Linq;
using static System.Console;

namespace FindConsole
{
    class Program
    {
        static IClient client;

        static void Main(string[] args)
        {
            SetupFind(showConfig: true);

            #region Actions

            //DeleteBooks();

            //DeletePeople();

            //IndexBooks();

            //IndexNorthwind();

            //AddBook1();
            //GetBook1();

            //AddPerson1();

            //AddStudent1();

            //SearchPeople();

            //UpdateBook1("The Lord of the Rings 2");
            //GetBook1();

            //ReplaceBook1("The Hunger Games");
            //GetBook1();

            //OutputBooks();

            OutputEverything();

            //SearchBooks();

            //SearchBooksFor("\"lord of the rings\"");
            //SearchBooksFor("ring lord");

            //SearchProducts();

            //SearchEverythingFor("mary");

            #endregion

            WriteLine("Press ENTER to end.");
            ReadLine();
        }

        #region Helper methods

        static void OutputIndexResult(IndexResult result)
        {
            WriteLine($"Index result [OK: {result.Ok}, Index: {result.Index}, Type: {result.Type}, Id: {result.Id}]");
        }

        static void OutputResults<T>(ITypeSearch<T> query)
        {
            int pageSize = 10;

            var pagedQuery = query.Take(pageSize);

            SearchResults<T> results = null;

            try
            {
                results = pagedQuery.GetResult();
            }
            catch (Exception ex)
            {
                WriteLine($"{ex.GetType()} says {ex.Message}");
                return;
            }

            int numberOfPages = (int)((double)(results.TotalMatching - 1) / pageSize) + 1;

            WriteLine($"Total matching: {results.TotalMatching}");
            WriteLine($"Server duration: {results.ProcessingInfo.ServerDuration}");
            WriteLine($"Shards [Successful: {results.ProcessingInfo.Shards.Successful}, Failed: {results.ProcessingInfo.Shards.Failed}]");
            WriteLine($"Timed out: {results.ProcessingInfo.TimedOut}");
            WriteLine();

            if (results.TotalMatching == 0)
            {
                WriteLine("There are no results to output.");
            }
            else
            {
                for (int page = 1; page <= numberOfPages; page++)
                {
                    WriteLine($"[ Page {page} of {numberOfPages} ]");
                    WriteLine();
                    OutputPageOfResults(results);
                    pagedQuery = query.Skip(page * pageSize).Take(pageSize);

                    try
                    {
                        results = pagedQuery.GetResult();
                    }
                    catch (Exception ex)
                    {
                        WriteLine($"{ex.GetType()} says {ex.Message}");
                        return;
                    }
                }
            }
        }

        static void OutputPageOfResults<T>(SearchResults<T> results)
        {
            foreach (SearchHit<T> hit in results.Hits)
            {
                Write($"Score: {hit.Score}, ");
                if (hit.Document is Book)
                {
                    var b = hit.Document as Book;
                    WriteLine($"Book ID: {b.Id}, Title: {b.Title}, Author: {b.Author}, Description: {b.Description}");
                }
                else if (hit.Document is Person)
                {
                    var p = hit.Document as Person;
                    var s = hit.Document as Student;

                    if (s != null) Write("Student"); else Write("Person");
                    Write($" ID: {p.PersonID}, Full name: {p.FirstName} {p.LastName}");

                    if (s != null) Write($", Course: {s.Course}");

                    WriteLine();
                }
                else if (hit.Document is Category)
                {
                    var c = hit.Document as Category;
                    WriteLine($"Category ID: {c.CategoryID}, Name: {c.CategoryName}, Description: {c.Description}");
                }
                else if (hit.Document is Product)
                {
                    var p = hit.Document as Product;
                    WriteLine($"Product ID: {p.ProductID}, Name: {p.ProductName}, Price: {p.UnitPrice}, Discontinued: {p.Discontinued}, Category: {p.Category?.CategoryName}, Supplier: {p.Supplier?.CompanyName}");
                }
                else
                {
                    WriteLine($"unknown type: {hit.Type}");
                }
                WriteLine();
            }
            WriteLine();
        }

        #endregion

        #region Indexing methods

        static void IndexBooks()
        {
            IndexResult result;

            WriteLine("* Indexing books");
            foreach (Book item in BookRepository.Books)
            {
                result = client.Index(item);
                OutputIndexResult(result);
            }
            WriteLine();
        }

        static void IndexNorthwind()
        {
            client.Conventions.ForInstancesOf<Category>().IdIs(cat => cat.CategoryID);
            client.Conventions.ForInstancesOf<Category>().ExcludeField(cat => cat.Products);

            client.Conventions.ForInstancesOf<Product>().IdIs(prod => prod.ProductID);
            client.Conventions.ForInstancesOf<Product>().ExcludeField(prod => prod.Order_Details);

            client.Conventions.ForInstancesOf<Customer>().IdIs(cust => cust.CustomerID);
            client.Conventions.ForInstancesOf<Customer>().ExcludeField(cust => cust.Orders);
            client.Conventions.ForInstancesOf<Customer>().ExcludeField(cust => cust.CustomerDemographics);

            client.Conventions.ForInstancesOf<Employee>().IdIs(emp => emp.EmployeeID);
            client.Conventions.ForInstancesOf<Employee>().ExcludeField(emp => emp.Employee1);
            client.Conventions.ForInstancesOf<Employee>().ExcludeField(emp => emp.Employees1);
            client.Conventions.ForInstancesOf<Employee>().ExcludeField(emp => emp.Photo);
            client.Conventions.ForInstancesOf<Employee>().ExcludeField(emp => emp.Orders);
            client.Conventions.ForInstancesOf<Employee>().ExcludeField(emp => emp.Territories);

            client.Conventions.ForInstancesOf<Supplier>().IdIs(sup => sup.SupplierID);
            client.Conventions.ForInstancesOf<Supplier>().ExcludeField(sup => sup.Products);

            client.Conventions.ForInstancesOf<Shipper>().IdIs(ship => ship.ShipperID);
            client.Conventions.ForInstancesOf<Shipper>().ExcludeField(ship => ship.Orders);

            var db = new Northwind();
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;

            IndexResult result;

            WriteLine("* Indexing categories");
            foreach (Category item in db.Categories)
            {
                result = client.Index(item);
                OutputIndexResult(result);
            }
            WriteLine();

            WriteLine("* Indexing suppliers");
            foreach (Supplier item in db.Suppliers)
            {
                result = client.Index(item);
                OutputIndexResult(result);
            }
            WriteLine();

            WriteLine("* Indexing products (including related supplier and category)");
            foreach (Product item in db.Products.Include(p => p.Supplier).Include(p => p.Category))
            {
                result = client.Index(item);
                OutputIndexResult(result);
            }
            WriteLine();

            WriteLine("* Indexing customers");
            foreach (Customer item in db.Customers)
            {
                result = client.Index(item);
                OutputIndexResult(result);
            }
            WriteLine();

            WriteLine("* Indexing employees");
            foreach (Employee item in db.Employees)
            {
                result = client.Index(item);
                OutputIndexResult(result);
            }
            WriteLine();
        }

        #endregion

        #region Task methods

        static void SetupFind(bool showConfig = false)
        {
            //DocumentId a = 1;
            //DocumentId b = Guid.NewGuid();
            //DocumentId c = DateTime.Now;
            //DocumentId d = "hello_world";

            WriteLine("* Configuring Episerver Find");

            client = Client.CreateFromConfig();

            //client = new Client(serviceUrl: "",
            //    defaultIndex: "",
            //    defaultRequestTimeout: 10);

            if (showConfig)
            {
                WriteLine($"  Default index: {client.DefaultIndex}");
                //WriteLine($"  URL: {client.ServiceUrl}");
                WriteLine($"  Admin: {client.Settings.Admin}");
                WriteLine($"  Connectors: {client.Settings.Connectors}");
                WriteLine($"  Max. documents: {client.Settings.MaxDocs}");
                WriteLine($"  Statistics: {client.Settings.Stats}");
                WriteLine($"  Version: {client.Settings.Version}");
                WriteLine($"  Status: {client.GetSettings().Status}");
                WriteLine($"  Supports these languages:");
                foreach (Language lang in client.Settings.Languages)
                {
                    WriteLine($"    {lang.Name}");
                }
            }

            WriteLine();
        }

        static void AddBook1()
        {
            WriteLine("* Adding book 50");

            var book = new Book
            {
                Id = 50,
                Title = "Blah",
                Description = "The Lord of the Rings is an epic high fantasy novel.",
                Author = "J. R. R. Tolkien"
            };

            var result = client.Index(book);/*, command =>
                {
                    command.Refresh = true; // so it appears in results immediately
                    command.TimeToLive = TimeSpan.FromMinutes(30);
                    command.Id = book.Id; // manually set the DocumentId for the item
                });*/

            OutputIndexResult(result);
            WriteLine();
        }

        static void AddPerson1()
        {
            WriteLine("* Adding person 1");
            var person = new Person
            {
                PersonID = 1,
                FirstName = "John",
                LastName = "Smith"
            };
            var result = client.Index(person, x => x.Refresh = true);
            OutputIndexResult(result);
            WriteLine();
        }

        private static void AddStudent1()
        {
            WriteLine("* Adding student 1");
            var student = new Student
            {
                PersonID = 1,
                FirstName = "Mary",
                LastName = "Jones",
                Course = "Episerver Find for Developers"
            };
            var result = client.Index(student, x => x.Refresh = true);
            OutputIndexResult(result);
            WriteLine();
        }

        static void GetBook1()
        {
            WriteLine("* Getting book 1");
            var book = client.Get<Book>(1);
            WriteLine($"ID: {book.Id}");
            WriteLine($"Title: {book.Title}");
            WriteLine($"Author: {book.Author}");
            WriteLine($"Description: {book.Description}");
            WriteLine();
        }

        static void ReplaceBook1(string newTitle)
        {
            WriteLine("* Replacing book 1");
            Book book = client.Get<Book>(1);
            book.Title = newTitle;
            IndexResult result = client.Index(book, x => x.Refresh = true);
            OutputIndexResult(result);
            WriteLine();
        }

        static void UpdateBook1(string newTitle)
        {
            WriteLine("* Updating book 1");
            ITypeUpdate<Book> updater = client.Update<Book>(1);
            ITypeUpdated<Book> command = updater.Field(x => x.Title, newTitle);
            IndexResult result = command.Execute();
            OutputIndexResult(result);
            WriteLine();
        }

        static void OutputBooks()
        {
            WriteLine($"* Show all books");

            // only look for "books"
            ITypeSearch<Book> search = client.Search<Book>();

            OutputResults(search);
        }

        static void OutputEverything()
        {
            WriteLine($"* Show everything");

            // look for every type that derives from object! 
            ITypeSearch<object> search = client.Search<object>();

            OutputResults(search);
        }

        static void SearchBooksFor(string q)
        {
            WriteLine($"* Search for books with: {q}");

            // only look for "books" and allow word stemming
            ITypeSearch<Book> search = client.Search<Book>(Language.English);

            // perform a "free text" query looking in Title and Description
            IQueriedSearch<Book> query = search.For(q)
                .InField(x => x.Title, 3.0) // triple the weighting for matches in Title
                .InField(x => x.Description);

            OutputResults(query as ITypeSearch<Book>);
        }

        static void SearchEverythingFor(string q)
        {
            WriteLine($"* Search for everything with: {q}");

            ITypeSearch<object> search = client.Search<object>();

            // perform a "free text" query
            IQueriedSearch<object> query = search.For(q);

            OutputResults(query as ITypeSearch<object>);
        }

        static void SearchBooks()
        {
            SearchBooksFor("lord");

            SearchBooksFor("lord of the rings");

            SearchBooksFor("\"lord of the rings\"");

            SearchBooksFor("stories");

            SearchBooksFor("politics");

            SearchBooksFor("time and space");
        }

        static void SearchPeople()
        {
            WriteLine("* Search for people");

            ITypeSearch<Person> search = client.Search<Person>();

            OutputResults(search);
        }

        static void SearchProducts()
        {
            WriteLine("* Search for products that are Beverages OR they are discontinued");

            ITypeSearch<Product> search = client.Search<Product>();

            search = search.Filter(x => x.Discontinued.Match(true));

            // change to Filter to do AND instead of OR
            search = search.Filter(x => x.Category.CategoryName.Match("Beverages"));

            OutputResults(search);

            WriteLine("* Search for products that are supplied by company starts with \"new\"");

            ITypeSearch<Product> search2 = client.Search<Product>();

            search = search.Filter(x => x.Supplier.CompanyName.PrefixCaseInsensitive("new"));

            OutputResults(search);
        }

        #endregion

        #region Clean up

        public static void DeleteBooks()
        {
            DeleteByQueryResult result = client.Delete<Book>(b => b.Id.Exists());
            WriteLine($"Delete all books. Ok: {result.Ok}");
        }

        public static void DeletePeople()
        {
            DeleteByQueryResult result = client.Delete<Person>(p => p.PersonID.Exists());
            WriteLine($"Delete all people. Ok: {result.Ok}");
        }

        #endregion
    }
}
