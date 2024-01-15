using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectNet.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
namespace ProjectNet.Controllers;

public class AdminController : Controller
{    
    private readonly ILogger<AdminController> _logger;
    // Add a private variable of type MyContext (or whatever you named your context file)
    private MyContext _context;         
    // Here we can "inject" our context service into the constructor 
    // The "logger" was something that was already in our code, we're just adding around it   
    private readonly IWebHostEnvironment _environment;
    // Here we can "inject" our context service into the constructor 
    // The "logger" was something that was already in our code, we're just adding around it   
    public AdminController(ILogger<AdminController> logger, MyContext context, IWebHostEnvironment environment)
    {
        _logger = logger;
        // When our HomeController is instantiated, it will fill in _context with context
        // Remember that when context is initialized, it brings in everything we need from DbContext
        // which comes from Entity Framework Core
        _context = context;
        _environment = environment;

    }

    [SessionCheck]
[AdminCheck]
public IActionResult Index(int page = 1)
{
    int pageSize = 10; // Number of items to display per page

    // Retrieve the products with associated categories
    IQueryable<Product> productsQuery = _context.Products.Include(e => e.AllAssociations).ThenInclude(e=> e.category).Include(e=>e.Purchases).OrderByDescending(e => e.Purchases.Count());;

    

    // Pagination
    var totalProducts = productsQuery.Count();
    var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

    // Ensure the requested page is within the valid range
    page = Math.Max(1, Math.Min(page, totalPages));

    // Apply pagination to the query
    var products = productsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    // Pass the paginated and sorted data to the view
    var viewModel = new PaginatedProductViewModel
    {
        Products = products,
        PageNumber = page,
        TotalPages = totalPages,
    };

    return View(viewModel);
}


    [AdminCheck]
    [HttpGet("newproduct")]
    public IActionResult NewProduct()
    {
        DataTwo DataTwo = new DataTwo();
        DataTwo.Categories= _context.Categories.ToList();
        return View(DataTwo);
    }
    
    [AdminCheck]
    [HttpPost("registerproduct")]
    public async Task<IActionResult> RegisterProduct(Product product)
{
    if (ModelState.IsValid)
    {
        if (product.ImageFile != null && product.ImageFile.Length > 0)
        {
            Console.WriteLine("U ekzekutua");
            // Process the uploaded file
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await product.ImageFile.CopyToAsync(fileStream);
            }

            // Update the model properties with the file details
            product.ImageFileName = uniqueFileName;
            product.ImageData = System.IO.File.ReadAllBytes(filePath);
        }
            _context.Products.Add(product);
            _context.SaveChanges();
            Association association = new Association();
             association.ProductId = product.ProductId;
            association.CategoryId = product.CategoryId;
            _context.Associations.Add(association);
            _context.SaveChanges();
            return RedirectToAction("Index");
           }
   
         ViewBag.AllProducts = _context.Products.ToList();
        DataTwo DataTwo = new DataTwo();
        DataTwo.Categories= _context.Categories.ToList();
        return View("NewProduct", DataTwo);

}

    [SessionCheck]
[AdminCheck]
[HttpGet("delete/{id}")]
public IActionResult DeleteItem(int id)
{
    Product product = _context.Products.Include(e => e.AllAssociations).FirstOrDefault(e => e.ProductId == id);
    List<Association> associations = _context.Associations.Where(e => e.ProductId == id).ToList();
    List<Purchase> purchases = _context.Purchases.Where(p => p.ProductId == id).ToList();
    _context.Purchases.RemoveRange(purchases);
    _context.Associations.RemoveRange(associations);
    
    _context.Products.Remove(product);

    _context.SaveChanges();

    return RedirectToAction("Index");
}

    [SessionCheck]
    [AdminCheck]
    [HttpGet("item/edit/{id}")]
    public IActionResult EditItem(int id)
    {
        DataTwo DataTwo = new DataTwo();
        DataTwo.Categories= _context.Categories.ToList();
        DataTwo.Product = _context.Products.FirstOrDefault(e => e.ProductId == id);
        return View(DataTwo);
    }


    [SessionCheck]
    [HttpPost("item/post/edit/{id}")]

public IActionResult EditProduct(Product productt, int id)
{
    if (ModelState.IsValid)
    {
        // Find the product from the database
        Product productFromDb = _context.Products.Include(e => e.AllAssociations)
                                                .ThenInclude(e => e.category)
                                                .FirstOrDefault(e => e.ProductId == id);

        
            // Update product properties
            productFromDb.Name = productt.Name;
            productFromDb.Brand = productt.Brand;
            productFromDb.Price = productt.Price;
            productFromDb.Quantity = productt.Quantity;
            productFromDb.Description = productt.Description;
            productFromDb.UpdatedAt = DateTime.Now;

            Association assoc = productFromDb.AllAssociations.FirstOrDefault(e=> e.ProductId == productt.ProductId);
                assoc.CategoryId = productt.CategoryId;
            _context.SaveChanges();

            return RedirectToAction("Shop");
        
    }

    // If ModelState is not valid or if the product is not found
    DataTwo dataTwo = new DataTwo();
    dataTwo.Categories = _context.Categories.ToList();
    dataTwo.Product = productt;

    return View("EditItem", dataTwo);
}

[AdminCheck]
    [HttpPost("registercategory")]
    public IActionResult RegisterCategory(Category category)
    {
        if (ModelState.IsValid)
        {
            _context.Add(category);
            _context.SaveChanges();
            return RedirectToAction("NewProduct");
        }
        ViewBag.AllCategories = _context.Categories.ToList();
        DataTwo DataTwo = new DataTwo();
        DataTwo.Categories= _context.Categories.ToList();
        return View("NewProduct",DataTwo);
    }

    [AdminCheck]
[HttpGet("showusers")]
public IActionResult ShowUsers(int page = 1)
{
    int pageSize = 10; // Number of items to display per page

    // Retrieve the users ordered by points
    IQueryable<UserReg> usersQuery = _context.Users.OrderByDescending(e => e.Points);

    // Pagination
    var totalUsers = usersQuery.Count();
    var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

    // Ensure the requested page is within the valid range
    page = Math.Max(1, Math.Min(page, totalPages));

    // Apply pagination to the query
    var users = usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    // Pass the paginated data to the view
    var viewModel = new PaginatedProductViewModel
    {
        Users = users,
        PageNumber = page,
        TotalPages = totalPages,
    };

    return View(viewModel);
}



[AdminCheck]
[HttpGet("showpurchases")]
public IActionResult ShowPurchases(int page = 1)
{
    int pageSize = 10; // Number of items to display per page

    // Retrieve the purchases with associated products and users
    IQueryable<Purchase> purchasesQuery = _context.Purchases.Include(e => e.Product).Include(e => e.User).OrderByDescending(e => e.PurchaseId);

    // Pagination
    var totalPurchases = purchasesQuery.Count();
    var totalPages = (int)Math.Ceiling((double)totalPurchases / pageSize);

    // Ensure the requested page is within the valid range
    page = Math.Max(1, Math.Min(page, totalPages));

    // Apply pagination to the query
    var purchases = purchasesQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();


    // Pass the paginated data to the view
    var viewModel = new PaginatedProductViewModel
    {
        Purchases = purchases,
        PageNumber = page,
        TotalPages = totalPages,
    };

    return View(viewModel);
}


}


