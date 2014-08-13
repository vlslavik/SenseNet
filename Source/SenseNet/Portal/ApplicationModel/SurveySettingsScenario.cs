﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ApplicationModel
{
    [Scenario("SurveySettings")]
    class SurveySettingsScenario : GenericScenario
    {
        protected override IEnumerable<ActionBase> CollectActions(Content context, string backUrl)
        {
            return base.CollectActions(context, backUrl).ToList();
        }
    }
}
