using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartTestDataGenerator.Core.Entities;
using SmartTestDataGenerator.Core.Enums;

namespace SmartTestDataGenerator.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Seed Settings if not present
            if (!context.Settings.Any())
            {
                context.Settings.AddRange(new List<Setting>
                {
                    new Setting { Key = "Theme", Value = "dark" },
                    new Setting { Key = "Language", Value = "tr" },
                    new Setting { Key = "DefaultExport", Value = "JSON" },
                    new Setting { Key = "DefaultSeed", Value = "42" }
                });
                context.SaveChanges();
            }

            // Seed Templates if not present
            if (!context.Templates.Any())
            {
                var templates = GetSystemTemplates();
                context.Templates.AddRange(templates);
                context.SaveChanges();

                // Now wire up foreign keys
                WireUpForeignKeys(context);
                context.SaveChanges();

                // Add initial activity log
                context.RecentActivities.Add(new RecentActivity
                {
                    ActivityType = "SystemInit",
                    Details = "Sistem başarıyla kuruldu ve hazır şablonlar eklendi.",
                    Timestamp = DateTime.UtcNow
                });
                context.SaveChanges();
            }
        }

        private static List<Template> GetSystemTemplates()
        {
            var templates = new List<Template>();

            // 1. BANKING TEMPLATE
            var banking = new Template
            {
                Name = "Bankacılık (Banking)",
                Description = "Müşteriler, banka hesapları, kredi kartları, kredi başvuruları ve hesap hareketleri içeren finansal veri şablonu.",
                IsSystem = true,
                Category = "Finans",
                IsPinned = true,
                IsFavorite = true
            };

            var custTable = new TemplateTable { Name = "Customers", RecordCount = 100, Order = 1 };
            custTable.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "FirstName", DataType = ColumnDataType.Name, Order = 1 },
                new TemplateColumn { Name = "LastName", DataType = ColumnDataType.Surname, Order = 2 },
                new TemplateColumn { Name = "Gender", DataType = ColumnDataType.Gender, Order = 3 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 4 },
                new TemplateColumn { Name = "Phone", DataType = ColumnDataType.Phone, Order = 5 },
                new TemplateColumn { Name = "BirthDate", DataType = ColumnDataType.BirthDate, MinRange = "1960-01-01", MaxRange = "2005-01-01", Order = 6 },
                new TemplateColumn { Name = "City", DataType = ColumnDataType.City, Order = 7 },
                new TemplateColumn { Name = "Country", DataType = ColumnDataType.Country, Order = 8 }
            };

            var accTable = new TemplateTable { Name = "Accounts", RecordCount = 150, Order = 2 };
            accTable.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "CustomerId", DataType = ColumnDataType.ForeignKey, Order = 1 }, // Will wire up FK later
                new TemplateColumn { Name = "IBAN", DataType = ColumnDataType.IBAN, Order = 2 },
                new TemplateColumn { Name = "Balance", DataType = ColumnDataType.Price, MinRange = "100", MaxRange = "100000", Order = 3 },
                new TemplateColumn { Name = "Currency", DataType = ColumnDataType.Currency, Order = 4 },
                new TemplateColumn { Name = "IsActive", DataType = ColumnDataType.Boolean, Order = 5 }
            };

            var cardTable = new TemplateTable { Name = "Cards", RecordCount = 120, Order = 3 };
            cardTable.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "AccountId", DataType = ColumnDataType.ForeignKey, Order = 1 }, // Will wire up FK
                new TemplateColumn { Name = "CardNumber", DataType = ColumnDataType.CreditCard, Order = 2 },
                new TemplateColumn { Name = "ExpiryDate", DataType = ColumnDataType.DateTime, MinRange = "2026-01-01", MaxRange = "2031-12-31", Order = 3 },
                new TemplateColumn { Name = "CVV", DataType = ColumnDataType.RandomNumber, MinRange = "100", MaxRange = "999", Order = 4 }
            };

            var txTable = new TemplateTable { Name = "Transactions", RecordCount = 300, Order = 4 };
            txTable.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "AccountId", DataType = ColumnDataType.ForeignKey, Order = 1 }, // Will wire up FK
                new TemplateColumn { Name = "TransactionDate", DataType = ColumnDataType.DateTime, MinRange = "2025-01-01", MaxRange = "2026-07-01", Order = 2 },
                new TemplateColumn { Name = "Amount", DataType = ColumnDataType.Price, MinRange = "5", MaxRange = "5000", Order = 3 },
                new TemplateColumn { Name = "Description", DataType = ColumnDataType.RandomText, CustomRule = "Para Transferi,Alışveriş,Fatura Ödemesi,EFT,Maaş Yatırma", Order = 4 }
            };

            var loanTable = new TemplateTable { Name = "Loans", RecordCount = 50, Order = 5 };
            loanTable.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "CustomerId", DataType = ColumnDataType.ForeignKey, Order = 1 }, // Will wire up FK
                new TemplateColumn { Name = "LoanAmount", DataType = ColumnDataType.Price, MinRange = "10000", MaxRange = "250000", Order = 2 },
                new TemplateColumn { Name = "InterestRate", DataType = ColumnDataType.Price, MinRange = "1.2", MaxRange = "5.5", Order = 3 },
                new TemplateColumn { Name = "TermMonths", DataType = ColumnDataType.RandomNumber, MinRange = "12", MaxRange = "120", Order = 4 }
            };

            banking.Tables.Add(custTable);
            banking.Tables.Add(accTable);
            banking.Tables.Add(cardTable);
            banking.Tables.Add(txTable);
            banking.Tables.Add(loanTable);
            templates.Add(banking);

            // 2. E-COMMERCE TEMPLATE
            var ecommerce = new Template
            {
                Name = "E-Ticaret (E-Commerce)",
                Description = "Müşteriler, ürünler, kategoriler, siparişler, sipariş detayları ve ürün değerlendirmelerini içeren ticaret şablonu.",
                IsSystem = true,
                Category = "Ticaret",
                IsPinned = true,
                IsFavorite = false
            };

            var ecCust = new TemplateTable { Name = "Customers", RecordCount = 100, Order = 1 };
            ecCust.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "FullName", DataType = ColumnDataType.FullName, Order = 1 },
                new TemplateColumn { Name = "Username", DataType = ColumnDataType.Username, Order = 2 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 3 },
                new TemplateColumn { Name = "Password", DataType = ColumnDataType.Password, Order = 4 },
                new TemplateColumn { Name = "Address", DataType = ColumnDataType.Address, Order = 5 },
                new TemplateColumn { Name = "City", DataType = ColumnDataType.City, Order = 6 },
                new TemplateColumn { Name = "ZipCode", DataType = ColumnDataType.ZipCode, Order = 7 }
            };

            var ecCat = new TemplateTable { Name = "Categories", RecordCount = 10, Order = 2 };
            ecCat.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "CategoryName", DataType = ColumnDataType.Category, Order = 1 },
                new TemplateColumn { Name = "Description", DataType = ColumnDataType.LoremIpsum, Order = 2 }
            };

            var ecProd = new TemplateTable { Name = "Products", RecordCount = 50, Order = 3 };
            ecProd.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "CategoryId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "ProductName", DataType = ColumnDataType.ProductName, Order = 2 },
                new TemplateColumn { Name = "Price", DataType = ColumnDataType.Price, MinRange = "10", MaxRange = "5000", Order = 3 },
                new TemplateColumn { Name = "Stock", DataType = ColumnDataType.RandomNumber, MinRange = "0", MaxRange = "500", Order = 4 },
                new TemplateColumn { Name = "Barcode", DataType = ColumnDataType.Barcode, Order = 5 }
            };

            var ecOrd = new TemplateTable { Name = "Orders", RecordCount = 150, Order = 4 };
            ecOrd.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "CustomerId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "OrderDate", DataType = ColumnDataType.DateTime, MinRange = "2025-01-01", MaxRange = "2026-07-01", Order = 2 },
                new TemplateColumn { Name = "OrderStatus", DataType = ColumnDataType.RandomText, CustomRule = "Pending,Shipped,Delivered,Cancelled", Order = 3 },
                new TemplateColumn { Name = "ShippingCost", DataType = ColumnDataType.Price, MinRange = "15", MaxRange = "150", Order = 4 }
            };

            var ecOrdItem = new TemplateTable { Name = "OrderItems", RecordCount = 300, Order = 5 };
            ecOrdItem.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "OrderId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "ProductId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "Quantity", DataType = ColumnDataType.RandomNumber, MinRange = "1", MaxRange = "5", Order = 3 },
                new TemplateColumn { Name = "UnitPrice", DataType = ColumnDataType.Price, MinRange = "10", MaxRange = "5000", Order = 4 }
            };

            var ecRev = new TemplateTable { Name = "Reviews", RecordCount = 80, Order = 6 };
            ecRev.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "ProductId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "CustomerId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "Rating", DataType = ColumnDataType.RandomNumber, MinRange = "1", MaxRange = "5", Order = 3 },
                new TemplateColumn { Name = "Comment", DataType = ColumnDataType.LoremIpsum, Order = 4 }
            };

            ecommerce.Tables.Add(ecCust);
            ecommerce.Tables.Add(ecCat);
            ecommerce.Tables.Add(ecProd);
            ecommerce.Tables.Add(ecOrd);
            ecommerce.Tables.Add(ecOrdItem);
            ecommerce.Tables.Add(ecRev);
            templates.Add(ecommerce);

            // 3. HOSPITAL TEMPLATE
            var hospital = new Template
            {
                Name = "Hastane (Hospital)",
                Description = "Bölümler, doktorlar, hastalar, randevular ve reçete kayıtlarını içeren sağlık sektörü şablonu.",
                IsSystem = true,
                Category = "Sağlık"
            };

            var hospDept = new TemplateTable { Name = "Departments", RecordCount = 8, Order = 1 };
            hospDept.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.RandomText, CustomRule = "Kardiyoloji,Nöroloji,Ortopedi,Pediatri,Dahiliye,Göz Hastalıkları,Dermatoloji,KBB", Order = 1 }
            };

            var hospDoc = new TemplateTable { Name = "Doctors", RecordCount = 20, Order = 2 };
            hospDoc.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "DepartmentId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.Name, Order = 2 },
                new TemplateColumn { Name = "Surname", DataType = ColumnDataType.Surname, Order = 3 },
                new TemplateColumn { Name = "Phone", DataType = ColumnDataType.Phone, Order = 4 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 5 }
            };

            var hospPat = new TemplateTable { Name = "Patients", RecordCount = 100, Order = 3 };
            hospPat.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "FirstName", DataType = ColumnDataType.Name, Order = 1 },
                new TemplateColumn { Name = "LastName", DataType = ColumnDataType.Surname, Order = 2 },
                new TemplateColumn { Name = "BloodType", DataType = ColumnDataType.RandomText, CustomRule = "A+,A-,B+,B-,AB+,AB-,O+,O-", Order = 3 },
                new TemplateColumn { Name = "BirthDate", DataType = ColumnDataType.BirthDate, MinRange = "1940-01-01", MaxRange = "2020-01-01", Order = 4 },
                new TemplateColumn { Name = "Phone", DataType = ColumnDataType.Phone, Order = 5 }
            };

            var hospApp = new TemplateTable { Name = "Appointments", RecordCount = 150, Order = 4 };
            hospApp.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "DoctorId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "PatientId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "AppointmentDate", DataType = ColumnDataType.DateTime, MinRange = "2026-01-01", MaxRange = "2026-12-31", Order = 3 },
                new TemplateColumn { Name = "Diagnosis", DataType = ColumnDataType.LoremIpsum, Order = 4 }
            };

            hospital.Tables.Add(hospDept);
            hospital.Tables.Add(hospDoc);
            hospital.Tables.Add(hospPat);
            hospital.Tables.Add(hospApp);
            templates.Add(hospital);

            // 4. UNIVERSITY TEMPLATE
            var university = new Template
            {
                Name = "Üniversite (University)",
                Description = "Akademik bölümler, akademisyenler, öğrenciler, dersler, notlar ve devam durumu şablonu.",
                IsSystem = true,
                Category = "Eğitim"
            };

            var uniDept = new TemplateTable { Name = "Departments", RecordCount = 6, Order = 1 };
            uniDept.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.RandomText, CustomRule = "Bilgisayar Mühendisliği,Makine Mühendisliği,Elektrik-Elektronik,Tıp Fakültesi,Hukuk,İktisat", Order = 1 }
            };

            var uniTeach = new TemplateTable { Name = "Teachers", RecordCount = 15, Order = 2 };
            uniTeach.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "DepartmentId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.Name, Order = 2 },
                new TemplateColumn { Name = "Surname", DataType = ColumnDataType.Surname, Order = 3 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 4 }
            };

            var uniCourse = new TemplateTable { Name = "Courses", RecordCount = 20, Order = 3 };
            uniCourse.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "DepartmentId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "TeacherId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "CourseName", DataType = ColumnDataType.RandomText, CustomRule = "Algoritmalar 101,Veri Yapıları,Yapay Zeka Giriş,Fizik 1,Calculus 1,Mikroiktisat", Order = 3 },
                new TemplateColumn { Name = "Credits", DataType = ColumnDataType.RandomNumber, MinRange = "2", MaxRange = "6", Order = 4 }
            };

            var uniStud = new TemplateTable { Name = "Students", RecordCount = 100, Order = 4 };
            uniStud.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "DepartmentId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "FirstName", DataType = ColumnDataType.Name, Order = 2 },
                new TemplateColumn { Name = "LastName", DataType = ColumnDataType.Surname, Order = 3 },
                new TemplateColumn { Name = "StudentNumber", DataType = ColumnDataType.RandomNumber, MinRange = "202000000", MaxRange = "202599999", Order = 4 }
            };

            var uniGrade = new TemplateTable { Name = "Grades", RecordCount = 200, Order = 5 };
            uniGrade.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "CourseId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "StudentId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "Score", DataType = ColumnDataType.RandomNumber, MinRange = "0", MaxRange = "100", Order = 3 }
            };

            university.Tables.Add(uniDept);
            university.Tables.Add(uniTeach);
            university.Tables.Add(uniCourse);
            university.Tables.Add(uniStud);
            university.Tables.Add(uniGrade);
            templates.Add(university);

            // 5. HUMAN RESOURCES TEMPLATE
            var hr = new Template
            {
                Name = "İnsan Kaynakları (HR)",
                Description = "Departmanlar, çalışanlar, maaş geçmişi, izin talepleri ve performans değerlendirmelerini içeren IK şablonu.",
                IsSystem = true,
                Category = "Kurumsal"
            };

            var hrDept = new TemplateTable { Name = "Departments", RecordCount = 6, Order = 1 };
            hrDept.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.Department, Order = 1 }
            };

            var hrEmp = new TemplateTable { Name = "Employees", RecordCount = 50, Order = 2 };
            hrEmp.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "DepartmentId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "FirstName", DataType = ColumnDataType.Name, Order = 2 },
                new TemplateColumn { Name = "LastName", DataType = ColumnDataType.Surname, Order = 3 },
                new TemplateColumn { Name = "JobTitle", DataType = ColumnDataType.JobTitle, Order = 4 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 5 },
                new TemplateColumn { Name = "HireDate", DataType = ColumnDataType.DateTime, MinRange = "2015-01-01", MaxRange = "2026-01-01", Order = 6 }
            };

            var hrSal = new TemplateTable { Name = "Salaries", RecordCount = 80, Order = 3 };
            hrSal.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "EmployeeId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "SalaryAmount", DataType = ColumnDataType.Salary, MinRange = "30000", MaxRange = "150000", Order = 2 },
                new TemplateColumn { Name = "EffectiveDate", DataType = ColumnDataType.DateTime, MinRange = "2020-01-01", MaxRange = "2026-07-01", Order = 3 }
            };

            var hrLeave = new TemplateTable { Name = "LeaveRequests", RecordCount = 40, Order = 4 };
            hrLeave.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "EmployeeId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "StartDate", DataType = ColumnDataType.DateTime, MinRange = "2025-01-01", MaxRange = "2026-12-31", Order = 2 },
                new TemplateColumn { Name = "EndDate", DataType = ColumnDataType.DateTime, MinRange = "2025-01-01", MaxRange = "2026-12-31", Order = 3 },
                new TemplateColumn { Name = "LeaveType", DataType = ColumnDataType.RandomText, CustomRule = "Yıllık İzin,Hastalık İzni,Mazeret İzni", Order = 4 }
            };

            hr.Tables.Add(hrDept);
            hr.Tables.Add(hrEmp);
            hr.Tables.Add(hrSal);
            hr.Tables.Add(hrLeave);
            templates.Add(hr);

            // 6. RESTAURANT TEMPLATE
            var rest = new Template
            {
                Name = "Restoran (Restaurant)",
                Description = "Müşteriler, menüler, siparişler, masalar ve ödeme kayıtlarını içeren yeme-içme şablonu.",
                IsSystem = true,
                Category = "Yeme-İçme"
            };

            var restCust = new TemplateTable { Name = "Customers", RecordCount = 80, Order = 1 };
            restCust.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.Name, Order = 1 },
                new TemplateColumn { Name = "Phone", DataType = ColumnDataType.Phone, Order = 2 }
            };

            var restMenu = new TemplateTable { Name = "Menus", RecordCount = 15, Order = 2 };
            restMenu.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "ItemName", DataType = ColumnDataType.RandomText, CustomRule = "Köfte,Adana Kebap,Pizzas,Hamburger,Sezar Salata,Mercimek Çorbası,Baklava,Künefe,Ayran,Kola", Order = 1 },
                new TemplateColumn { Name = "Price", DataType = ColumnDataType.Price, MinRange = "50", MaxRange = "800", Order = 2 },
                new TemplateColumn { Name = "Category", DataType = ColumnDataType.RandomText, CustomRule = "Ana Yemek,Tatlı,Çorba,İçecek,Salata", Order = 3 }
            };

            var restOrd = new TemplateTable { Name = "Orders", RecordCount = 120, Order = 3 };
            restOrd.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "CustomerId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "OrderDate", DataType = ColumnDataType.DateTime, MinRange = "2026-06-01", MaxRange = "2026-07-15", Order = 2 },
                new TemplateColumn { Name = "TableNumber", DataType = ColumnDataType.RandomNumber, MinRange = "1", MaxRange = "30", Order = 3 }
            };

            var restPay = new TemplateTable { Name = "Payments", RecordCount = 120, Order = 4 };
            restPay.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "OrderId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "Amount", DataType = ColumnDataType.Price, MinRange = "50", MaxRange = "3000", Order = 2 },
                new TemplateColumn { Name = "PaymentMethod", DataType = ColumnDataType.RandomText, CustomRule = "Nakit,Kredi Kartı,Yemek Kartı", Order = 3 }
            };

            rest.Tables.Add(restCust);
            rest.Tables.Add(restMenu);
            rest.Tables.Add(restOrd);
            rest.Tables.Add(restPay);
            templates.Add(rest);

            // 7. LIBRARY TEMPLATE
            var lib = new Template
            {
                Name = "Kütüphane (Library)",
                Description = "Yazarlar, kitaplar, kütüphane üyeleri ve ödünç alma kayıtlarını barındıran şablon.",
                IsSystem = true,
                Category = "Kültür"
            };

            var libAuth = new TemplateTable { Name = "Authors", RecordCount = 20, Order = 1 };
            libAuth.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.Name, Order = 1 },
                new TemplateColumn { Name = "Surname", DataType = ColumnDataType.Surname, Order = 2 },
                new TemplateColumn { Name = "BirthCountry", DataType = ColumnDataType.Country, Order = 3 }
            };

            var libBook = new TemplateTable { Name = "Books", RecordCount = 50, Order = 2 };
            libBook.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "AuthorId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "Title", DataType = ColumnDataType.RandomText, CustomRule = "Nutuk,Kürk Mantolu Madonna,İnce Memed,Tutunamayanlar,Çalıkuşu,Sefiller,Suç ve Ceza", Order = 2 },
                new TemplateColumn { Name = "ISBN", DataType = ColumnDataType.ISBN, Order = 3 },
                new TemplateColumn { Name = "PublishYear", DataType = ColumnDataType.RandomNumber, MinRange = "1800", MaxRange = "2024", Order = 4 }
            };

            var libMem = new TemplateTable { Name = "Members", RecordCount = 60, Order = 3 };
            libMem.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "Name", DataType = ColumnDataType.FullName, Order = 1 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 2 },
                new TemplateColumn { Name = "RegistrationDate", DataType = ColumnDataType.DateTime, MinRange = "2020-01-01", MaxRange = "2026-01-01", Order = 3 }
            };

            var libBorrow = new TemplateTable { Name = "BorrowRecords", RecordCount = 100, Order = 4 };
            libBorrow.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "BookId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "MemberId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "BorrowDate", DataType = ColumnDataType.DateTime, MinRange = "2026-01-01", MaxRange = "2026-07-01", Order = 3 },
                new TemplateColumn { Name = "ReturnDate", DataType = ColumnDataType.DateTime, IsNullable = true, NullPercentage = 30, MinRange = "2026-01-01", MaxRange = "2026-07-01", Order = 4 }
            };

            lib.Tables.Add(libAuth);
            lib.Tables.Add(libBook);
            lib.Tables.Add(libMem);
            lib.Tables.Add(libBorrow);
            templates.Add(lib);

            // 8. HOTEL TEMPLATE
            var hotel = new Template
            {
                Name = "Otel (Hotel)",
                Description = "Misafirler, oda tanımları, rezervasyonlar ve ödeme işlemlerini kapsayan turizm şablonu.",
                IsSystem = true,
                Category = "Turizm"
            };

            var hotGuest = new TemplateTable { Name = "Guests", RecordCount = 50, Order = 1 };
            hotGuest.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "FirstName", DataType = ColumnDataType.Name, Order = 1 },
                new TemplateColumn { Name = "LastName", DataType = ColumnDataType.Surname, Order = 2 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 3 },
                new TemplateColumn { Name = "Phone", DataType = ColumnDataType.Phone, Order = 4 },
                new TemplateColumn { Name = "Country", DataType = ColumnDataType.Country, Order = 5 }
            };

            var hotRoom = new TemplateTable { Name = "Rooms", RecordCount = 30, Order = 2 };
            hotRoom.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "RoomNumber", DataType = ColumnDataType.RandomNumber, MinRange = "101", MaxRange = "510", Order = 1 },
                new TemplateColumn { Name = "RoomType", DataType = ColumnDataType.RandomText, CustomRule = "Single,Double,Suite,Family", Order = 2 },
                new TemplateColumn { Name = "PricePerNight", DataType = ColumnDataType.Price, MinRange = "800", MaxRange = "8000", Order = 3 }
            };

            var hotRes = new TemplateTable { Name = "Reservations", RecordCount = 80, Order = 3 };
            hotRes.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "GuestId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "RoomId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "CheckInDate", DataType = ColumnDataType.DateTime, MinRange = "2026-01-01", MaxRange = "2026-07-01", Order = 3 },
                new TemplateColumn { Name = "CheckOutDate", DataType = ColumnDataType.DateTime, MinRange = "2026-01-01", MaxRange = "2026-07-01", Order = 4 }
            };

            hotel.Tables.Add(hotGuest);
            hotel.Tables.Add(hotRoom);
            hotel.Tables.Add(hotRes);
            templates.Add(hotel);

            // 9. SOCIAL MEDIA TEMPLATE
            var social = new Template
            {
                Name = "Sosyal Medya (Social Media)",
                Description = "Kullanıcılar, gönderiler, beğeniler, takipçi ilişkileri ve yorumları içeren sosyal ağ şablonu.",
                IsSystem = true,
                Category = "Sosyal Ağ",
                IsPinned = false,
                IsFavorite = true
            };

            var socUser = new TemplateTable { Name = "Users", RecordCount = 80, Order = 1 };
            socUser.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "Username", DataType = ColumnDataType.Username, Order = 1 },
                new TemplateColumn { Name = "FullName", DataType = ColumnDataType.FullName, Order = 2 },
                new TemplateColumn { Name = "Email", DataType = ColumnDataType.Email, Order = 3 },
                new TemplateColumn { Name = "Password", DataType = ColumnDataType.Password, Order = 4 },
                new TemplateColumn { Name = "ProfileImage", DataType = ColumnDataType.ImageUrl, Order = 5 },
                new TemplateColumn { Name = "Bio", DataType = ColumnDataType.RandomText, CustomRule = "Gezgin,Yazılımcı,Fotoğrafçı,Kitap kurdu,Müzik aşığı", Order = 6 }
            };

            var socPost = new TemplateTable { Name = "Posts", RecordCount = 150, Order = 2 };
            socPost.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "UserId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "Content", DataType = ColumnDataType.LoremIpsum, Order = 2 },
                new TemplateColumn { Name = "PostDate", DataType = ColumnDataType.DateTime, MinRange = "2025-01-01", MaxRange = "2026-07-15", Order = 3 },
                new TemplateColumn { Name = "LikeCount", DataType = ColumnDataType.RandomNumber, MinRange = "0", MaxRange = "1000", Order = 4 }
            };

            var socComm = new TemplateTable { Name = "Comments", RecordCount = 200, Order = 3 };
            socComm.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "PostId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "UserId", DataType = ColumnDataType.ForeignKey, Order = 2 },
                new TemplateColumn { Name = "CommentText", DataType = ColumnDataType.LoremIpsum, Order = 3 },
                new TemplateColumn { Name = "CommentDate", DataType = ColumnDataType.DateTime, MinRange = "2025-01-01", MaxRange = "2026-07-15", Order = 4 }
            };

            var socFollow = new TemplateTable { Name = "Followers", RecordCount = 120, Order = 4 };
            socFollow.Columns = new List<TemplateColumn>
            {
                new TemplateColumn { Name = "UserId", DataType = ColumnDataType.ForeignKey, Order = 1 },
                new TemplateColumn { Name = "FollowerId", DataType = ColumnDataType.ForeignKey, Order = 2 }
            };

            social.Tables.Add(socUser);
            social.Tables.Add(socPost);
            social.Tables.Add(socComm);
            social.Tables.Add(socFollow);
            templates.Add(social);

            return templates;
        }

        private static void WireUpForeignKeys(AppDbContext context)
        {
            // Now load the generated records from DB to reference their IDs
            var templates = context.Templates
                .Include(t => t.Tables)
                    .ThenInclude(tb => tb.Columns)
                .ToList();

            foreach (var template in templates)
            {
                if (template.Name.Contains("Bankacılık"))
                {
                    var custTable = template.Tables.First(tb => tb.Name == "Customers");
                    var accTable = template.Tables.First(tb => tb.Name == "Accounts");
                    var cardTable = template.Tables.First(tb => tb.Name == "Cards");
                    var txTable = template.Tables.First(tb => tb.Name == "Transactions");
                    var loanTable = template.Tables.First(tb => tb.Name == "Loans");

                    // CustomerId in Accounts references Customers
                    var fkCustInAcc = accTable.Columns.First(c => c.Name == "CustomerId");
                    fkCustInAcc.ParentTableId = custTable.Id;

                    // AccountId in Cards references Accounts
                    var fkAccInCard = cardTable.Columns.First(c => c.Name == "AccountId");
                    fkAccInCard.ParentTableId = accTable.Id;

                    // AccountId in Transactions references Accounts
                    var fkAccInTx = txTable.Columns.First(c => c.Name == "AccountId");
                    fkAccInTx.ParentTableId = accTable.Id;

                    // CustomerId in Loans references Customers
                    var fkCustInLoan = loanTable.Columns.First(c => c.Name == "CustomerId");
                    fkCustInLoan.ParentTableId = custTable.Id;
                }
                else if (template.Name.Contains("E-Ticaret"))
                {
                    var custTable = template.Tables.First(tb => tb.Name == "Customers");
                    var catTable = template.Tables.First(tb => tb.Name == "Categories");
                    var prodTable = template.Tables.First(tb => tb.Name == "Products");
                    var ordTable = template.Tables.First(tb => tb.Name == "Orders");
                    var ordItemTable = template.Tables.First(tb => tb.Name == "OrderItems");
                    var revTable = template.Tables.First(tb => tb.Name == "Reviews");

                    // CategoryId in Products references Categories
                    prodTable.Columns.First(c => c.Name == "CategoryId").ParentTableId = catTable.Id;

                    // CustomerId in Orders references Customers
                    ordTable.Columns.First(c => c.Name == "CustomerId").ParentTableId = custTable.Id;

                    // OrderId and ProductId in OrderItems
                    ordItemTable.Columns.First(c => c.Name == "OrderId").ParentTableId = ordTable.Id;
                    ordItemTable.Columns.First(c => c.Name == "ProductId").ParentTableId = prodTable.Id;

                    // ProductId and CustomerId in Reviews
                    revTable.Columns.First(c => c.Name == "ProductId").ParentTableId = prodTable.Id;
                    revTable.Columns.First(c => c.Name == "CustomerId").ParentTableId = custTable.Id;
                }
                else if (template.Name.Contains("Hastane"))
                {
                    var deptTable = template.Tables.First(tb => tb.Name == "Departments");
                    var docTable = template.Tables.First(tb => tb.Name == "Doctors");
                    var patTable = template.Tables.First(tb => tb.Name == "Patients");
                    var appTable = template.Tables.First(tb => tb.Name == "Appointments");

                    // DepartmentId in Doctors
                    docTable.Columns.First(c => c.Name == "DepartmentId").ParentTableId = deptTable.Id;

                    // DoctorId and PatientId in Appointments
                    appTable.Columns.First(c => c.Name == "DoctorId").ParentTableId = docTable.Id;
                    appTable.Columns.First(c => c.Name == "PatientId").ParentTableId = patTable.Id;
                }
                else if (template.Name.Contains("Üniversite"))
                {
                    var deptTable = template.Tables.First(tb => tb.Name == "Departments");
                    var teachTable = template.Tables.First(tb => tb.Name == "Teachers");
                    var courseTable = template.Tables.First(tb => tb.Name == "Courses");
                    var studTable = template.Tables.First(tb => tb.Name == "Students");
                    var gradeTable = template.Tables.First(tb => tb.Name == "Grades");

                    // DepartmentId in Teachers
                    teachTable.Columns.First(c => c.Name == "DepartmentId").ParentTableId = deptTable.Id;

                    // DepartmentId and TeacherId in Courses
                    courseTable.Columns.First(c => c.Name == "DepartmentId").ParentTableId = deptTable.Id;
                    courseTable.Columns.First(c => c.Name == "TeacherId").ParentTableId = teachTable.Id;

                    // DepartmentId in Students
                    studTable.Columns.First(c => c.Name == "DepartmentId").ParentTableId = deptTable.Id;

                    // CourseId and StudentId in Grades
                    gradeTable.Columns.First(c => c.Name == "CourseId").ParentTableId = courseTable.Id;
                    gradeTable.Columns.First(c => c.Name == "StudentId").ParentTableId = studTable.Id;
                }
                else if (template.Name.Contains("İnsan Kaynakları"))
                {
                    var deptTable = template.Tables.First(tb => tb.Name == "Departments");
                    var empTable = template.Tables.First(tb => tb.Name == "Employees");
                    var salTable = template.Tables.First(tb => tb.Name == "Salaries");
                    var leaveTable = template.Tables.First(tb => tb.Name == "LeaveRequests");

                    // DepartmentId in Employees
                    empTable.Columns.First(c => c.Name == "DepartmentId").ParentTableId = deptTable.Id;

                    // EmployeeId in Salaries
                    salTable.Columns.First(c => c.Name == "EmployeeId").ParentTableId = empTable.Id;

                    // EmployeeId in LeaveRequests
                    leaveTable.Columns.First(c => c.Name == "EmployeeId").ParentTableId = empTable.Id;
                }
                else if (template.Name.Contains("Restoran"))
                {
                    var custTable = template.Tables.First(tb => tb.Name == "Customers");
                    var menuTable = template.Tables.First(tb => tb.Name == "Menus");
                    var ordTable = template.Tables.First(tb => tb.Name == "Orders");
                    var payTable = template.Tables.First(tb => tb.Name == "Payments");

                    // CustomerId in Orders
                    ordTable.Columns.First(c => c.Name == "CustomerId").ParentTableId = custTable.Id;

                    // OrderId in Payments
                    payTable.Columns.First(c => c.Name == "OrderId").ParentTableId = ordTable.Id;
                }
                else if (template.Name.Contains("Kütüphane"))
                {
                    var authTable = template.Tables.First(tb => tb.Name == "Authors");
                    var bookTable = template.Tables.First(tb => tb.Name == "Books");
                    var memTable = template.Tables.First(tb => tb.Name == "Members");
                    var borrowTable = template.Tables.First(tb => tb.Name == "BorrowRecords");

                    // AuthorId in Books
                    bookTable.Columns.First(c => c.Name == "AuthorId").ParentTableId = authTable.Id;

                    // BookId and MemberId in BorrowRecords
                    borrowTable.Columns.First(c => c.Name == "BookId").ParentTableId = bookTable.Id;
                    borrowTable.Columns.First(c => c.Name == "MemberId").ParentTableId = memTable.Id;
                }
                else if (template.Name.Contains("Otel"))
                {
                    var guestTable = template.Tables.First(tb => tb.Name == "Guests");
                    var roomTable = template.Tables.First(tb => tb.Name == "Rooms");
                    var resTable = template.Tables.First(tb => tb.Name == "Reservations");

                    // GuestId and RoomId in Reservations
                    resTable.Columns.First(c => c.Name == "GuestId").ParentTableId = guestTable.Id;
                    resTable.Columns.First(c => c.Name == "RoomId").ParentTableId = roomTable.Id;
                }
                else if (template.Name.Contains("Sosyal Medya"))
                {
                    var userTable = template.Tables.First(tb => tb.Name == "Users");
                    var postTable = template.Tables.First(tb => tb.Name == "Posts");
                    var commTable = template.Tables.First(tb => tb.Name == "Comments");
                    var followTable = template.Tables.First(tb => tb.Name == "Followers");

                    // UserId in Posts
                    postTable.Columns.First(c => c.Name == "UserId").ParentTableId = userTable.Id;

                    // PostId and UserId in Comments
                    commTable.Columns.First(c => c.Name == "PostId").ParentTableId = postTable.Id;
                    commTable.Columns.First(c => c.Name == "UserId").ParentTableId = userTable.Id;

                    // UserId and FollowerId in Followers
                    followTable.Columns.First(c => c.Name == "UserId").ParentTableId = userTable.Id;
                    followTable.Columns.First(c => c.Name == "FollowerId").ParentTableId = userTable.Id;
                }
            }
        }
    }
}
