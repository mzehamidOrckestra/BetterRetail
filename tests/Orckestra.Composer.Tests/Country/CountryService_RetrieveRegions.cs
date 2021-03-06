﻿using System;
using FizzWare.NBuilder.Generators;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Orckestra.Composer.Country;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Providers.Localization;
using Orckestra.Composer.Tests.Mock;
using Orckestra.ForTests;

namespace Orckestra.Composer.Tests.Country
{
    [TestFixture]
    public class CountryServiceRetrieveRegions
    {
        private AutoMocker _container;

        [SetUp]
        public void SetUp()
        {
            //Arrange
            _container = new AutoMocker();
        }

        [Test]
        public void WHEN_Passing_Valid_Parameters_SHOULD_Succeed()
        {
            //Arrange
            _container.Use(ViewModelMapperFactory.Create());
            _container.Use(CountryRepositoryFactory.Create());

            var localizationProviderMock = new Mock<ILocalizationProvider>();
            localizationProviderMock
                .Setup(c => c.GetLocalizedString(It.IsAny<GetLocalizedParam>())).Returns("{0}");

            _container.Use(localizationProviderMock);

            var service = _container.CreateInstance<CountryService>();

            // Act
            var result = service.RetrieveRegionsAsync(new RetrieveCountryParam
            {
                IsoCode = GetRandom.String(32),
                CultureInfo = TestingExtensions.GetRandomCulture(),
            }).Result;

            // Assert
            result.Should().NotBeNull();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t\r\n")]
        public void WHEN_IsoCode_Is_NullOrWhitespace_SHOULD_Throw_ArgumentException(string isoCode)
        {
            //Arrange
            _container.Use(ViewModelMapperFactory.Create());
            _container.Use(CountryRepositoryFactory.Create());
            var service = _container.CreateInstance<CountryService>();
            var param = new RetrieveCountryParam
            {
                IsoCode = isoCode,
                CultureInfo = TestingExtensions.GetRandomCulture()
            };

            // Act
            var exception = Assert.ThrowsAsync<ArgumentException>(() => service.RetrieveRegionsAsync(param));

            //Assert
            exception.ParamName.Should().BeSameAs("param");
        }

        [Test]
        public void WHEN_CultureInfo_Is_Null_SHOULD_Throw_ArgumentException()
        {
            // Arrange
            _container.Use(ViewModelMapperFactory.Create());
            _container.Use(CountryRepositoryFactory.Create());
            var service = _container.CreateInstance<CountryService>();

            var param = new RetrieveCountryParam
            {
                IsoCode = GetRandom.String(32),
            };

            // Act
            var exception = Assert.ThrowsAsync<ArgumentException>(() => service.RetrieveRegionsAsync(param));

            //Assert
            exception.ParamName.Should().BeSameAs("param");
        }
    }
}
