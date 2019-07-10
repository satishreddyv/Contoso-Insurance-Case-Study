using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Revature.Plugins
{
    public class AccountPostUpdate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity account = (Entity)context.InputParameters["Target"];

                // Obtain the organization service reference which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);


                try
                {

                    string city = account.Attributes["address1_city"].ToString();


                    // query contacts related to current account
                    // using QueryExpression
                    QueryExpression query = new QueryExpression("contact");
                    query.ColumnSet.AddColumn("address1_city");
                    query.Criteria.AddCondition("parentcustomerid", ConditionOperator.Equal,
                        account.Id);


                    // using QueryByAttribute
                    // QueryByAttribute query2 = new QueryByAttribute("contact");
                    // query2.ColumnSet.AddColumn("address1_city");
                    // query2.AddAttributeValue("parentcustomerid", account.Id);

                    //                  string fetchxml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    //<entity name='contact'>
                    //  <attribute name='fullname' />
                    //  <attribute name='telephone1' />
                    //  <attribute name='contactid' />
                    //  <order attribute='fullname' descending='false' />
                    //  <filter type='and'>
                    //    <condition attribute='parentcustomerid' operator='eq' uitype='account' value='" + account.Id + @"' />
                    //  </filter>
                    //</entity>
                    //</ fetch > ";
                    //   EntityCollection collection = service.RetrieveMultiple(new FetchExpression(fetchxml));



                    EntityCollection collection = service.RetrieveMultiple(query);

                    foreach (Entity item in collection.Entities)
                    {
                        if (item.Attributes.Contains("address1_city"))
                        {
                            item.Attributes["address1_city"] = city;
                        }
                        else
                        {
                            item.Attributes.Add("address1_city", city);
                        }
                        service.Update(item);

                    }

                    string desc = string.Empty;
                    if (account.Attributes.Contains("description"))
                    {
                        desc = account.Attributes["description"].ToString();

                        account.Attributes["description"] = desc + "Account is updated with city " + city;

                    }
                    else
                    {
                        // User is not updating desc

                        Entity accountImage = context.PreEntityImages["PreImage"];

                        if (accountImage.Attributes.Contains("description"))
                        {
                            desc = accountImage.Attributes["description"].ToString();
                        }


                        //Entity retrieved = service.Retrieve("account", account.Id, new ColumnSet(new string[] { "description" }));
                        //if (retrieved.Attributes.Contains("description"))
                        //{
                        //    desc = retrieved.Attributes["description"].ToString();
                        //}

                        account.Attributes.Add("description", desc + "Account is updated with city " + city);
                    }

                    service.Update(account);
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in FollowUpPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("FollowUpPlugin: {0}", ex.ToString());
                    throw;
                }
            }

        }
    }
}
