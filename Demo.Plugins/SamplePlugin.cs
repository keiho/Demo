namespace SamplePlugin
{


using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using CCLLC.Core;
using CCLLC.CDS.Sdk;
using System.Collections.Generic;
using CCLLC.Core.Serialization;



    [CrmPluginRegistration(MessageNameEnum.Create, "account", StageEnum.PostOperation,
      ExecutionModeEnum.Synchronous, "", "Sample PostOp Account Create", 1001, IsolationModeEnum.Sandbox,
      Id = "542E2780-E569-4B95-988D-EC36C5238FF4")]
    [CrmPluginRegistration(MessageNameEnum.Update, "account", StageEnum.PostOperation,
      ExecutionModeEnum.Synchronous, "", "Sample PostOp Account Update", 1001, IsolationModeEnum.Sandbox,
      Id = "{3B694AE4-63EF-42A6-98A7-33771E784901}")]
    [CrmPluginRegistration(MessageNameEnum.Update, "none", StageEnum.PreOperation,
      ExecutionModeEnum.Synchronous, "", "Sample PreOp Entity Update", 1001, IsolationModeEnum.Sandbox,
      Id = "{A2EB72E2-3364-4A5A-8659-73D45947C428}")]

    public class PluginWithTelemetry : InstrumentedCDSPlugin
    {
        public PluginWithTelemetry(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            Container.Implement<IJSONContractSerializer>().Using<DateOnlyJSONSerializer>();

            this.RunAs = eRunAs.User;

            // DefaultInstrumentationKey (for InstrumetendCDSPlugin only)  - Sets the ApplicationInsights instrumentation key. This value is set in code 
            // for developer convenience but in practice it should  be set in a CDS Environment Variable named 'Telemetry.InstrumentationKey'
            this.DefaultInstrumentationKey = "ff202598-08a9-458b-b712-0d2032a602da";

            // Execute the workingWithEntities handler in the PreOpStage of an Account update.
            RegisterUpdateHandler<Account>(ePluginStage.PreOperation, workingWithEntityUpdate);

            // Execute the demonstrateWebRequest handler in the Post Operation stage of an account creation.
            RegisterEventHandler("account", MessageNames.Create, ePluginStage.PostOperation, demonstrateWebRequest);

            // Execute the workingWithOrgService handler in the Post Operation stage of an account update.
            RegisterEventHandler("account", MessageNames.Update, ePluginStage.PostOperation, workingWithOrgService);

            // Call demonstrateTelemetry handler in the PreOperation stage of any entity update. Send a specific id tag to telemetry.
            RegisterEventHandler(null, MessageNames.Update, ePluginStage.PreOperation, demonstrateTelemetry, "OnUpdateHandler_TelemetryDemonstration");
        }



        /// <summary>
        /// Demonstration update event handler
        /// </summary>
        /// <param name="executionContext">The <see cref="ICDSPluginExecutionContext"/> execution context for
        /// the handler execution</param>
        /// <param name="target">The target entity for the update. Contains any fields that are being sent in for update.</param>
        private void workingWithEntityUpdate(ICDSPluginExecutionContext executionContext, Account target)
        {

            // carry out actions only if the update is affecting the name or accountnumber fields
            if (target.ContainsAny("name", "accountnumber"))
            {
                executionContext.Trace("Target contains at least one matching attribute");  // short cut to tracing service Trace method.
            }

            // TargetReference - Provides and entity reference for the entity in the InputParameters Target parameter. Return null if the entity does not exist.
            EntityReference targetRef = executionContext.TargetReference;

            // PreImage - Provides access to the first entity image in the PreEntityImages collection. Returns null if a image does not exist. 
            Entity preImage = executionContext.PreImage;

            // PreMergedTarget - Provides an entity with attributes from the TargetEntity merged into the PreImage entity. If an attribute exists in 
            // both the TargetEntity and the PreImage, the merged target will show the attribute from the Target.
            var mergedTarget = executionContext.PreMergedTarget.ToEntity<Account>();


            // GetRecord is a simple retrieve shortcut that accepts an EntityReference, an optional list of columns, 
            // and/or a cache timeout value for caching the result, and returns an early-bound entity. 
            var myRecord1 = executionContext.GetRecord<Account>(targetRef); //Get record with all columns.
            var myRecord2 = executionContext.GetRecord<Account>(targetRef, new string[] { "name", "accountnumber" }); //retrieve 2 columns
            var myRecord3 = executionContext.GetRecord<Account>(targetRef, TimeSpan.FromSeconds(10)); //retrieve and cache for 10 seconds

        }

        private void workingWithSerialization(ICDSExecutionContext executionContext, Entity target)
        {
            //Get upto 50 account records.
            var records = executionContext.OrganizationService.Query<Account>().SelectAll().With.RecordLimit(50).RetrieveAll();

            Model.Registrations registrations = new Model.Registrations();

            foreach (var record in records)
            {
                registrations.Add(new Model.RegistrationData { Id = record.Id, CreatedOn = record.CreatedOn.Value });
            }

            var serializer = Container.Resolve<IJSONContractSerializer>();

            var serializedData = registrations.ToString(serializer);

        }

        /// <summary>
        /// Demonstrates a simple GET operation using built in web request support. Will automatically
        /// generate dependency telemetry.
        /// </summary>
        /// <param name="executionContext"></param>
        private void demonstrateWebRequest(ICDSPluginExecutionContext executionContext)
        {
            Uri googleUri = new Uri("https://www.google.com");
            using (var webRequest = executionContext.CreateWebRequest(googleUri))
            {
                webRequest.Headers.Add("");
                var result = webRequest.Get();
                executionContext.Trace("Returned {0} characters", result.Content.Length);
            }
        }


        /// <summary>
        /// Demonstrates various features associated with the Organization Service 
        /// </summary>
        /// <param name="executionContext"></param>
        private void workingWithOrgService(ICDSPluginExecutionContext executionContext)
        {
            // OrganizationService - Provides easy access to the organization service. 
            // This instance of the organization service runs as user or system based on
            // the plugin RunAs execution property.
            var orgService = executionContext.OrganizationService;

            // ElevatedOrganizationService - Provides easy access to an organization service
            // instance that runs under the security context of the System User regardless
            // of plugin RunAs execution property.
            var elevatedOrgService = executionContext.ElevatedOrganizationService;


            // FluentQuery OrganizationService extension generates more readable queries than
            // standard query syntax. 
            var records = orgService.Query<Account>()
                .Select(cols => new { cols.Name, cols.AccountNumber })
                .WhereAll(e => e
                    .IsActive()
                    .Attribute("name").Is<string>(ConditionOperator.BeginsWith, "C"))
                .OrderByAsc("name", "accountnumber")
                .Retrieve();

            var accountId = executionContext.TargetReference;

            // Load a record based on its record id            
            var lateBoundRecord = orgService.GetRecord(accountId, "accountid", "name", "accountnumber");

            var earlyBoundRecord = orgService.GetRecord<Account>(accountId, cols => new { cols.Id, cols.Name, cols.AccountNumber });


        }

        /// <summary>
        /// Demonstrates basic tracing and tracking methods built into the execution context.
        /// </summary>
        /// <param name="executionContext"></param>
        private void demonstrateTelemetry(ICDSPluginExecutionContext executionContext)
        {
            // Information message written to the standard tracing service and also to ApplicationInsights.
            executionContext.Trace("The primary entity type is '{0}'", executionContext.PrimaryEntityName);

            // Waring message written to the standard tracing service and also to ApplicationInsights.
            executionContext.Trace(eSeverityLevel.Warning, "A warning message for record '{0}'", executionContext.PrimaryEntityId);

            // Create an Event marker in ApplicationInsights
            executionContext.TrackEvent("Event_1");

            // Capture exception information for a handled exception in ApplicationInsights
            // Note that capturing exception telemetry for unhandled exceptions is taken care of
            // in the InstrumentedCDSPlugin abstract class.
            try
            {
                throw new Exception("Sample exception");
            }
            catch (Exception ex)
            {
                executionContext.TrackException(ex);
                // do something to handle exception

            }

        }
    }
}
