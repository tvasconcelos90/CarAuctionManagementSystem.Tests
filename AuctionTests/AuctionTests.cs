
using CarAuctionManagementSystem.AuctionSrc;
using CarAuctionManagementSystem.Models;
using FluentAssertions;

namespace CarAuctionManagementSystem.Tests.AuctionTests
{
    public class AuctionTests
    {
        private readonly AuctionInventory _auctionInventory;
        private readonly List<Auction> _auctions;
        private readonly AuctionService _auctionService;

        private static readonly Vehicle _vehicleHatchBack = new Hatchback
        {
            Id = 1,
            Manufacturer = "BMW",
            Model = "xpto",
            Year = 2007,
            StartingBid = 6000,
            NumberOfDoors = 5,
        };

        private static readonly Vehicle _vehicleSedan = new Sedan
        {
            Id = 2,
            Manufacturer = "BMW",
            Model = "xpto",
            Year = 2010,
            StartingBid = 8000,
            NumberOfDoors = 5,
        };

        public AuctionTests()
        {
            _auctionInventory = new AuctionInventory();
            _auctions = [];
            _auctionService = new AuctionService(_auctionInventory, _auctions);
        }

        [Fact]
        public void AuctionService_AddVehicle()
        {

            _auctionService.AddVehicle(_vehicleHatchBack);

            _auctionInventory.Vehicles.Should().NotBeEmpty();
            _auctionInventory.Vehicles.Should().Contain(_vehicleHatchBack);
        }

        [Fact]
        public void AuctionService_AddVehicle_With_Same_Id()
        {
            var vehicleWithSameId = new Sedan
            {
                Id = 1,
                Manufacturer = "BMW",
                Model = "xpto",
                Year = 2007,
                StartingBid = 6000,
                NumberOfDoors = 5,
            };

            _auctionService.AddVehicle(_vehicleHatchBack);

            _auctionService.Invoking(y => y.AddVehicle(vehicleWithSameId))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Vehicle Id: 1 already existis");
        }

        [Fact]
        public void AuctionService_SearchVehicles_Not_Found()
        {
            var searchedVehicles = _auctionService.SearchVehicles(new SearchVehicle { VehicleType = VehicleType.SUV });

            searchedVehicles.Should().NotBeNull();
            searchedVehicles.Should().BeEmpty();
        }

        [Theory]
        [InlineData(VehicleType.Sedan, null, null, null, 1)]
        [InlineData(null, "BMW", null, null, 2)]
        public void AuctionService_SearchVehicles_With_One_Search_Parameter(VehicleType? type, string? manufacturer, string? model, int? year, int count)
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.AddVehicle(_vehicleSedan);

            var search = new SearchVehicle
            {
                VehicleType = type,
                Model = model,
                Manufacturer = manufacturer,
                Year = year
            };

            var searchedVehicles = _auctionService.SearchVehicles(search);

            searchedVehicles.Should().HaveCount(count);

        }

        [Theory]
        [InlineData(null, "Toyota", null, 2020, 0)]
        [InlineData(null, "BMW", null, 2007, 1)]
        [InlineData(null, "BMW", "xpto", null, 2)]
        public void AuctionService_SearchVehicles_With_Many_Search_Parameters(VehicleType? type, string? manufacturer, string? model, int? year, int count)
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.AddVehicle(_vehicleSedan);

            var search = new SearchVehicle
            {
                VehicleType = type,
                Model = model,
                Manufacturer = manufacturer,
                Year = year
            };

            var searchedVehicles = _auctionService.SearchVehicles(search);

            searchedVehicles.Should().HaveCount(count);

        }

        [Fact]
        public void AuctionService_StartAnAuction_With_Vehicle_In_Inventory_And_Without_ActiveAuctions()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);

            _auctions.Should().HaveCount(1);
            _auctions[0].IsActive.Should().BeTrue();
            _auctions[0].Vehicle.Should().Be(_vehicleHatchBack);
        }

        [Fact]
        public void AuctionService_StartAnAuction_With_Vehicle_In_Inventory_And_Existing_In_Another_ActiveAuction()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);

            _auctionService.Invoking(y => y.StartAnAuction(_vehicleHatchBack))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Vehicle Id 1 is already in another auction.");
        }

        [Fact]
        public void AuctionService_CloseAnAuction_With_ActiveStatus_True()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);
            _auctionService.CloseTheAuction(_vehicleHatchBack);

            _auctions.Should().HaveCount(1);
            _auctions[0].IsActive.Should().BeFalse();
        }

        [Fact]
        public void AuctionService_CloseAnAuction_With_ActiveStatus_False()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);
            _auctionService.CloseTheAuction(_vehicleHatchBack);

            _auctionService.Invoking(y => y.CloseTheAuction(_vehicleHatchBack))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("There is no auction active for vehicle id 1.");
        }

        [Fact]
        public void AuctionService_CloseAnAuction_With_Vehicle_That_Has_No_Active_Auction()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);

            _auctionService.Invoking(y => y.CloseTheAuction(_vehicleSedan))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("There is no auction active for vehicle id 2.");
        }

        [Fact]
        public void AuctionService_PlaceABid_With_Value_Lower_Than_StartingBidValue_Of_Vehicle()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);

            _auctionService.Invoking(y => y.PlaceABid(_vehicleHatchBack, 1000))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Starting bid value 6000€ is greater than selected bid 1000€");
        }

        [Fact]
        public void AuctionService_PlaceABid_With_Value_Lower_Than_Current_Auction_Bid_Of_Vehicle()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);
            _auctionService.PlaceABid(_vehicleHatchBack, 6500);

            _auctionService.Invoking(y => y.PlaceABid(_vehicleHatchBack, 6200))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Current bid value 6500€ is greater than selected bid 6200€");
        }

        [Fact]
        public void AuctionService_PlaceABid_With_Value_Greater_Than_StartingBidValue_Of_Vehicle()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);
            _auctionService.PlaceABid(_vehicleHatchBack, 6100);

            _auctions[0].Should().NotBeNull();
            _auctions[0].Bid.Should().Be(6100);            
        }

        [Fact]
        public void AuctionService_PlaceABid_With_Value_Greater_Than_Current_Auction_Bid_Of_Vehicle()
        {
            _auctionService.AddVehicle(_vehicleHatchBack);
            _auctionService.StartAnAuction(_vehicleHatchBack);
            _auctionService.PlaceABid(_vehicleHatchBack, 6500);

            _auctionService.PlaceABid(_vehicleHatchBack, 6700);

            _auctions[0].Should().NotBeNull();
            _auctions[0].Bid.Should().Be(6700);
        }
    }
}
