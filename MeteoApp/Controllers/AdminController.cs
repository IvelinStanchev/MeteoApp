﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MeteoApp.Models.AdminViewModels;
using MeteoApp.Data;
using MeteoApp.Data.Models;
using System;

namespace MeteoApp.Controllers
{
    public class AdminController : Controller
    {
        public const string STATION_NAME_INVALID = "Invalid station name.";
        public const string STATION_ALREADY_EXISTS = "Station already exists.";
        public const string INVALID_WEIGHT_SUM = "The sum of weights must be equal to 1.";
        public const string START_CANNOT_BE_BEFORE_END = "The interval start cannot be after the interval end.";

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AddStation()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddStation(CreateStationViewModel stationToAdd)
        {
            var meteoData = new MeteoDataDBContext();

            var stationExists = meteoData
                .Stations
                .Any(station => station.Name.ToLower() == stationToAdd.StationName.ToLower());

            if (string.IsNullOrEmpty(stationToAdd.StationName))
            {
                ModelState.AddModelError(string.Empty, STATION_NAME_INVALID);
                return View();
            }

            if (!stationExists)
            {
                meteoData
                    .Stations
                    .Add(
                        new Station
                        {
                            Name = stationToAdd.StationName
                        }
                    );

                meteoData.SaveChanges();

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, STATION_ALREADY_EXISTS);

            return View();
        }

        [HttpGet]
        public IActionResult AddWeights()
        {
            var meteoData = new MeteoDataDBContext();

            var stationWeights = meteoData
                .Stations
                .Select(x => new StationWeightViewModel
                {
                    StationId = x.Id,
                    StationName = x.Name,
                    StationWeight = 0
                })
                .ToList();

            var viewModel = new AddStationWeightsViewModel
            {
                From = DateTime.Now,
                To = DateTime.Now,
                StationWeights = stationWeights
            };
            
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AddWeights(AddStationWeightsViewModel weightData)
        {
            var areWeightsValid = weightData.StationWeights.Sum(x => x.StationWeight) == 1;

            if (!areWeightsValid)
            {
                ModelState.AddModelError(string.Empty, INVALID_WEIGHT_SUM);

                return View(weightData);
            }

            if (weightData.From > weightData.To)
            {
                ModelState.AddModelError(string.Empty, START_CANNOT_BE_BEFORE_END);

                return View(weightData);
            }

            var dbStationWeights = weightData
                .StationWeights
                .Where(x => x.StationWeight > 0)
                .Select(x => new StationWeight
                {
                    From = weightData.From,
                    To = weightData.To,
                    StationId = x.StationId,
                    Weight = x.StationWeight
                });

            var meteoData = new MeteoDataDBContext();

            meteoData.StationsWeights.AddRange(dbStationWeights);
            meteoData.SaveChanges();

            return RedirectToAction("Index", "Home");
        }
    }
}
