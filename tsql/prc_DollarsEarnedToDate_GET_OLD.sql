IF EXISTS (SELECT * FROM sysobjects WHERE type = 'P' AND name = 'prc_DollarsEarnedToDate_GET')
		DROP  Procedure  dbo.prc_DollarsEarnedToDate_GET
GO

CREATE PROCEDURE dbo.prc_DollarsEarnedToDate_GET
	@fiscal_year	int,
	@sort_by	varchar(50)
AS
BEGIN
/******************************************************************************
**		File: 
**		Name: Stored_Procedure_Name
**		Desc: 
**
**		This template can be customized:
** 
**		Called by:   Report_Full.aspx
**
**		Auth: Nate Mow
**		Date: 5/5/2004
*******************************************************************************
**		Change History
*******************************************************************************
**		Date:		Author:				Description:
**		--------	--------			-------------------------------------------
**    		5/14/2004	Nate Mow			Applied dollar,date,isnull() formatting to the final SELECT statement
**		5/17/2004	Nate Mow			Added cast() statements to fix the final ORDER BY clause
**		
*******************************************************************************/

declare @distributor_cd varchar(20)
set	@distributor_cd = 'SIP'

set nocount on

select distinct 
	-- d.program_id
	--,q.quarter_id
	 q.fiscal_year
	--,q.quarter
	,q.close_dt
	--,p.full_nola_cd
	,p.re_up

-- 1) Program Title
	,p.program_title						as [SIP Program Title]


-- 2) Release Date
	,(select min(r2.rights_start_dt) from PROGRAM_RIGHTS r2 where r2.program_id = d.program_id)
									as [Release Date]

-- 3) SIP Investment
	,p.sip_investment						as [SIP Investment]


-- 4) Actual $ Earned
	,cast(	(	select	sum(isnull(d2.on_air_dollars,0) + isnull(d2.web_dollars,0))
			from	QDR_DETAIL d2 
			where	d2.quarter_id = q.quarter_id
			and	d2.program_id = d.program_id
		) as money)						as [Dollars Earned To Date - Actual]


-- 5) Extrap $ Earned
	,case	when (q.fiscal_year <= 2002) then
			--'Actual Dollars Earned + (Actual Dollars Earned * q.extrap_rate)'
			cast(	(	select (sum(isnull(d2.on_air_dollars,0) + isnull(d2.web_dollars,0)) + (	
						sum(isnull(d2.on_air_dollars,0) + isnull(d2.web_dollars,0)) * q.extrap_rate)
						)
					from	QDR_DETAIL d2 
					where	d2.quarter_id = q.quarter_id
					and	d2.program_id = d.program_id
				) as money)
		when (q.fiscal_year >= 2003 and p.distributor_cd  = 'SIP') then
			--'Actual Dollars Earned / p.SIP_ASF'
			cast(	(	sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) / 
					isnull(q.SIP_ASF,1)
				) as money)
		when (q.fiscal_year >= 2003 and p.distributor_cd != 'SIP') then
			--'Actual Dollars Earned / p.NPS_ASF'
			cast(	(	sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) / 
					isnull(q.NPS_ASF,1)
				) as money)
		else 0
		end							as [Dollars Earned To Date - Extrapolated]


-- 6) ROI To Date - Actual
	,case
	 when (p.sip_investment is not null and p.sip_investment > 0)
	 then (	(	select	sum(isnull(d2.on_air_dollars,0) + isnull(d2.web_dollars,0))
			from	QDR_DETAIL d2 
			where	d2.quarter_id = q.quarter_id
			and	d2.program_id = d.program_id ) 
			/ p.sip_investment )
	 else	0
	 end								as [ROI To Date - Actual]
	

-- 7) 'Extrap $ Earned / SIP_Investment' as [Extrap ROI to Date]
	,case
	 when (p.sip_investment is not null and p.sip_investment > 0)
	 then
		(	case	when (q.fiscal_year <= 2002) then
					--'Actual Dollars Earned + (Actual Dollars Earned * q.extrap_rate)'
					cast(	(	select (sum(isnull(d2.on_air_dollars,0) + isnull(d2.web_dollars,0)) + (	
								sum(isnull(d2.on_air_dollars,0) + isnull(d2.web_dollars,0)) * q.extrap_rate)
								)
							from	QDR_DETAIL d2 
							where	d2.quarter_id = q.quarter_id
							and	d2.program_id = d.program_id
						) as money)
				when (q.fiscal_year >= 2003 and p.distributor_cd  = 'SIP') then
					--'Actual Dollars Earned / p.SIP_ASF'
					cast(	(	sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) / 
							isnull(q.SIP_ASF,1)
						) as money)
				when (q.fiscal_year >= 2003 and p.distributor_cd <> 'SIP') then
					--'Actual Dollars Earned / p.NPS_ASF'
					cast(	(	sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) / 
							isnull(q.NPS_ASF,1)
						) as money)
				else 0
				end
		)
	 	/ p.sip_investment
	 else	0
	 end 								as [ROI To Date - Extrapolated]


-- 8) 'Actual $ Earned / Total Break Min' as [Actual $ Per Min]
	,case
	 when (		select	sum(isnull(d2.break_minutes,0)) 
			from	QDR_DETAIL d2 
			where	d2.quarter_id = q.quarter_id 
			and	d2.program_id = d.program_id 
		) > 0 
	 then
		 cast(	(	select	sum(isnull(d2.on_air_dollars,0) + isnull(d2.web_dollars,0))
				from	QDR_DETAIL d2 
				where	d2.quarter_id = q.quarter_id
				and	d2.program_id = d.program_id
				)
			/
			((	select	sum(isnull(d2.break_minutes,0)) 
				from	QDR_DETAIL d2 
				where	d2.quarter_id = q.quarter_id 
				and	d2.program_id = d.program_id )	/ 60)
		 as money)							
	 else	0
	 end								as [Dollars Per Minute]

into	#FINAL
from	PROGRAM 	p
join	PROGRAM_RIGHTS	r on p.program_id  = r.program_id
join	QDR_DETAIL	d on p.program_id  = d.program_id
join	QUARTER		q on d.quarter_id  = d.quarter_id
where	d.quarter_id			   = q.quarter_id
-- Return Past years --> @fiscal_year
and	q.fiscal_year			  <= @fiscal_year
-- Return @fiscal_year --> Present
-- and	q.fiscal_year			  >= @fiscal_year
-- and	q.fiscal_year			  <= (select max(q2.fiscal_year) from QUARTER q2)
and	p.distributor_cd		   = @distributor_cd
group by	 q.fiscal_year,q.quarter,q.close_dt
		,d.program_id
		--,p.full_nola_cd
		,ltrim(rtrim(substring(p.full_nola_cd,0,5)))
		,p.re_up,p.program_title,p.distributor_cd,p.sip_investment
		--,r.rights_start_dt
		,q.quarter_id,q.fiscal_year,q.extrap_rate,q.SIP_ASF,q.NPS_ASF
order by	 p.program_title
		,q.fiscal_year


-- Now apply the sort requested and formatting...
declare @sql nvarchar(4000)
select	@sql = 'select	 f.[SIP Program Title]
			,case
			 when (f.[Release Date] is not null) then dbo.fn_GetDateTimeString(f.[Release Date],''mmm-yy'')
			 else ''''
			 end														as [Release Date]
			,replace(dbo.fn_FormatNumber(round(isnull(f.[SIP Investment],0),0),2,1),''.00'','''')				as [SIP Investment]
			,replace(dbo.fn_FormatNumber(round(isnull(f.[Dollars Earned To Date - Actual],0),0),2,1),''.00'','''')		as [Dollars Earned To Date - Actual]
			,replace(dbo.fn_FormatNumber(round(isnull(f.[Dollars Earned To Date - Extrapolated],0),0),2,1),''.00'','''')	as [Dollars Earned To Date - Extrapolated]
			,case
			 when (f.[ROI To Date - Actual] = 0) then ''0''
			 else cast(round(f.[ROI To Date - Actual],2) as nvarchar(10))
			 end														as [ROI To Date - Actual]
			,case
			 when (f.[ROI To Date - Extrapolated] = 0) then ''0''
			 else cast(round(f.[ROI To Date - Extrapolated],2) as nvarchar(10))
			 end														as [ROI To Date - Extrapolated]
			,replace(dbo.fn_FormatNumber(round(isnull(f.[Dollars Per Minute],0),0),2,1),''.00'','''')			as [Dollars Per Minute]
		from #FINAL f 
		order by ' + 
	case
	when (@sort_by = 'Dollars Earned to Date') then
		'cast(f.[Dollars Earned To Date - Actual] as money),f.[SIP Program Title]'
	when (@sort_by = 'Dollars Per Minute') then
		'cast(f.[Dollars Per Minute] as money),f.[SIP Program Title]'
	when (@sort_by = 'Extrap Dollars Earned to Date') then
		'cast(f.[Dollars Earned To Date - Extrapolated] as money),f.[SIP Program Title]'
	when (@sort_by = 'Release Date') then
		'cast(f.[Release Date] as datetime) desc,f.[SIP Program Title]'
	when (@sort_by = 'ROI to date') then
		'cast(f.[ROI To Date - Actual] as float) desc,f.[SIP Program Title]'
	when (@sort_by = 'SIP Investment') then
		'cast(f.[SIP Investment] as money),f.[SIP Program Title]'
	else	'f.[SIP Program Title]'
	end

set nocount off

--print @sql
exec sp_executesql @sql
drop table #FINAL

END
GO

GRANT EXEC ON dbo.prc_DollarsEarnedToDate_GET TO PUBLIC
GO
