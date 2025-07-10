using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractCollaboratorInterfaceTests
{
    [Test]
    public async Task HandleZeroCollaborators()
    {
        const string code = """
                            public class OrderService
                            {
                                public void ProcessOrder(Order order)
                                {
                                    order.Status = "Processed";
                                }
                            }
                            """;

        await VerifyExtractCollaboratorInterface(code);
    }

    [Test]
    public async Task HandleOneCollaboratorOneMethod()
    {
        const string code = """
                            public class OrderService
                            {
                                private readonly PaymentProcessor _paymentProcessor;

                                public OrderService(PaymentProcessor paymentProcessor)
                                {
                                    _paymentProcessor = paymentProcessor;
                                }

                                public void ProcessOrder(Order order)
                                {
                                    _paymentProcessor.ProcessPayment();
                                }
                            }
                            """;

        await VerifyExtractCollaboratorInterface(code, "3:29-3:46");
    }

    [Test]
    public async Task HandleOneCollaboratorOneProperty()
    {
        const string code = """
                            public class OrderService
                            {
                                private readonly PaymentProcessor _paymentProcessor;

                                public OrderService(PaymentProcessor paymentProcessor)
                                {
                                    _paymentProcessor = paymentProcessor;
                                }

                                public void ProcessOrder(Order order)
                                {
                                    var status = _paymentProcessor.Status;
                                }
                            }
                            """;

        await VerifyExtractCollaboratorInterface(code, "3:29-3:46");
    }

    [Test]
    public async Task HandleOneCollaboratorMultipleMembers()
    {
        const string code = """
                            public class OrderService
                            {
                                private readonly PaymentProcessor _paymentProcessor;

                                public OrderService(PaymentProcessor paymentProcessor)
                                {
                                    _paymentProcessor = paymentProcessor;
                                }

                                public void ProcessOrder(Order order)
                                {
                                    _paymentProcessor.ProcessPayment();
                                    var status = _paymentProcessor.Status;
                                }
                            }
                            """;

        await VerifyExtractCollaboratorInterface(code, "3:29-3:46");
    }

    [Test]
    public async Task HandleCollaboratorWithExistingInterface()
    {
        const string code = """
                            public interface IPaymentProcessor
                            {
                                void ProcessPayment();
                            }

                            public class PaymentProcessor : IPaymentProcessor
                            {
                                public void ProcessPayment() { }
                            }

                            public class OrderService
                            {
                                private readonly PaymentProcessor _paymentProcessor;

                                public OrderService(PaymentProcessor paymentProcessor)
                                {
                                    _paymentProcessor = paymentProcessor;
                                }

                                public void ProcessOrder(Order order)
                                {
                                    _paymentProcessor.ProcessPayment();
                                }
                            }
                            """;

        await VerifyExtractCollaboratorInterface(code, "15:29-15:46");
    }

    [Test]
    public async Task HandlePropertyInjection()
    {
        const string code = """
                            public class OrderService
                            {
                                public PaymentProcessor PaymentProcessor { get; set; }

                                public void ProcessOrder(Order order)
                                {
                                    PaymentProcessor.ProcessPayment();
                                }
                            }
                            """;

        await VerifyExtractCollaboratorInterface(code, "3:21-3:38");
    }

    [Test]
    public async Task HandleConstructorWithMultipleDependencies()
    {
        const string code = """
                            public class OrderService
                            {
                                private readonly ILogger _logger;
                                private readonly PaymentProcessor _paymentProcessor;
                                private readonly IEmailService _emailService;

                                public OrderService(ILogger logger, PaymentProcessor paymentProcessor, IEmailService emailService)
                                {
                                    _logger = logger;
                                    _paymentProcessor = paymentProcessor;
                                    _emailService = emailService;
                                }

                                public void ProcessOrder(Order order)
                                {
                                    _logger.Log("Processing order");
                                    _paymentProcessor.ProcessPayment();
                                    _emailService.SendConfirmation();
                                }
                            }
                            """;

        await VerifyExtractCollaboratorInterface(code, "4:29-4:46");
    }

    private static async Task VerifyExtractCollaboratorInterface(string code, string selectionText = "")
    {
        var document = DocumentTestHelper.CreateDocument(code);

        var selection = string.IsNullOrEmpty(selectionText)
            ? CodeSelection.Parse("1:0-1:0")
            : CodeSelection.Parse(selectionText);

        var extractCollaboratorInterface = new ExtractCollaboratorInterface(selection);
        var updatedDocument = await extractCollaboratorInterface.PerformAsync(document);
        var formatted = Formatter.Format((await updatedDocument.GetSyntaxRootAsync())!, new AdhocWorkspace());
        await Verify(formatted.ToFullString());
    }
}
