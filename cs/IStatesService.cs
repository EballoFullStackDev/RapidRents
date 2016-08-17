using System.Collections.Generic;
using RapidRents.Web.Domain;
using RapidRents.Web.Domain.State;

namespace RapidRents.Web.Services.States
{
    public interface IStatesService
    {
        List<Territory> GetByCountryCode(string CountryRegionCode);
    }
}