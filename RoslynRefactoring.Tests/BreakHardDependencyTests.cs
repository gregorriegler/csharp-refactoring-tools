using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class BreakHardDependencyTests
{
    [Test]
    public async Task HandleZeroHardDependencies()
    {
        const string code = """
                            public class OrderProcessor
                            {
                                public void Process(Order order)
                                {
                                    order.Status = "Processed";
                                }
                            }
                            """;

        await VerifyBreakHardDependency(code);
    }

    [Test]
    public async Task HandleOneHardDependencyNoConstructor()
    {
        const string code = """
                            public class OrderProcessor
                            {
                                private OrderRepository _orderRepository = OrderRepository.Instance;

                                public void Process(Order order)
                                {
                                    _orderRepository.Save(order);
                                    order.Status = "Processed";
                                }
                            }
                            """;

        await VerifyBreakHardDependency(code, "31:33-31:58");
    }

    [Test]
    public async Task HandleOneHardDependencyWithExistingConstructor()
    {
        const string code = """
                            public class OrderProcessor
                            {
                                private OrderRepository _orderRepository = OrderRepository.Instance;
                                private readonly ProductCatalog _productCatalog;

                                public OrderProcessor(ProductCatalog productCatalog)
                                {
                                    _productCatalog = productCatalog;
                                }

                                public void Process(Order order)
                                {
                                    _orderRepository.Save(order);
                                    _productCatalog.Update(order.ProductId);
                                    order.Status = "Processed";
                                }
                            }
                            """;

        await VerifyBreakHardDependency(code, "50:33-50:58");
    }

    [Test]
    public async Task UpdateCallers()
    {
        const string code = """
                            public class OrderProcessor
                            {
                                private OrderRepository _orderRepository = OrderRepository.Instance;

                                public void Process(Order order)
                                {
                                    _orderRepository.Save(order);
                                    order.Status = "Processed";
                                }
                            }

                            public class OrderService
                            {
                                public void ProcessOrder(Order order)
                                {
                                    var processor = new OrderProcessor();
                                    processor.Process(order);
                                }
                            }
                            """;

        await VerifyBreakHardDependency(code, "76:33-76:58");
    }

    private static async Task VerifyBreakHardDependency(string code, string selectionText = "")
    {
        var document = DocumentTestHelper.CreateDocument(code);

        var selection = string.IsNullOrEmpty(selectionText)
            ? CodeSelection.Parse("1:0-1:0")
            : CodeSelection.Parse(selectionText);

        var breakHardDependency = new BreakHardDependency(selection);
        var updatedDocument = await breakHardDependency.PerformAsync(document);
        var formatted = Formatter.Format((await updatedDocument.GetSyntaxRootAsync())!, new AdhocWorkspace());
        await Verify(formatted.ToFullString());
    }
}
