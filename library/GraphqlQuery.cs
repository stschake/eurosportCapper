using System;

namespace esnew
{

    public static class GraphqlQuery
    {
        public const string AiringsOnNow = "{ onNow: query(index: \"eurosport_global_on_now\", type: \"Airing\", page_size: 500) @context(uiLang: \"en\") { hits { hit { ... on Airing {type contentId mediaId liveBroadcast linear partnerProgramId programId runTime startDate endDate expires genres playbackUrls { href rel templated } channel { id parent callsign partnerId } photos { id uri width height } mediaConfig { state productType type } titles { language title descriptionLong descriptionShort episodeName } } } } } }";
    }

}