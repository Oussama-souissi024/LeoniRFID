-- ═══════════════════════════════════════════════════════════════
-- SCHÉMA COMPLET LeoniRFID — Déploiement PC Étudiante
-- ═══════════════════════════════════════════════════════════════

-- 1. Nettoyage des anciennes tables et triggers
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
DROP FUNCTION IF EXISTS public.handle_new_user CASCADE;

DROP TABLE IF EXISTS public.scan_events CASCADE;
DROP TABLE IF EXISTS public.maintenance_sessions CASCADE;
DROP TABLE IF EXISTS public.machines CASCADE;
DROP TABLE IF EXISTS public.departments CASCADE;
DROP TABLE IF EXISTS public.profiles CASCADE;

-- 2. Création des tables
CREATE TABLE IF NOT EXISTS public.profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    full_name TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT 'Technician' CHECK (role IN ('Admin', 'Technician', 'Maintenance')),
    is_active BOOLEAN NOT NULL DEFAULT true,
    must_change_password BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.departments (
    id SERIAL PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL DEFAULT '',
    description TEXT
);

CREATE TABLE IF NOT EXISTS public.machines (
    id SERIAL PRIMARY KEY,
    tag_reference TEXT NOT NULL UNIQUE,
    standard_equipment_name TEXT NOT NULL DEFAULT '',
    plant TEXT NOT NULL DEFAULT '',
    area TEXT NOT NULL DEFAULT '',
    serial_number TEXT NOT NULL DEFAULT '',
    immobilisation_number TEXT NOT NULL DEFAULT '',
    cao_number TEXT,
    year_of_construction INTEGER NOT NULL DEFAULT 2020,
    equipment_status TEXT NOT NULL DEFAULT 'Active' CHECK (equipment_status IN ('Active','Passive','Defect','Scrapped','TransferDone','TransferOngoing','TransferAvailable','InMaintenance')),
    notes TEXT,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.scan_events (
    id SERIAL PRIMARY KEY,
    tag_id TEXT NOT NULL DEFAULT '',
    machine_id INTEGER REFERENCES public.machines(id),
    user_id UUID REFERENCES auth.users(id),
    event_type TEXT NOT NULL DEFAULT 'Scan',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
    notes TEXT
);

CREATE TABLE IF NOT EXISTS public.maintenance_sessions (
    id SERIAL PRIMARY KEY,
    machine_id INTEGER NOT NULL REFERENCES public.machines(id),
    technician_id UUID REFERENCES auth.users(id),
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ended_at TIMESTAMPTZ,
    duration_minutes DOUBLE PRECISION,
    notes TEXT
);

-- 3. Trigger pour création automatique de profil
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO public.profiles (id, full_name, role, must_change_password)
    VALUES (
        NEW.id,
        COALESCE(NEW.raw_user_meta_data->>'full_name', ''),
        COALESCE(NEW.raw_user_meta_data->>'role', 'Technician'),
        true
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- 4. Configuration RLS (Row Level Security)
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.machines ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.scan_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.maintenance_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.departments ENABLE ROW LEVEL SECURITY;

CREATE POLICY p1 ON public.profiles FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY p2 ON public.profiles FOR UPDATE USING (auth.uid() = id);
CREATE POLICY p3 ON public.machines FOR ALL USING (auth.role() = 'authenticated');
CREATE POLICY p4 ON public.scan_events FOR ALL USING (auth.role() = 'authenticated');
CREATE POLICY p5 ON public.maintenance_sessions FOR ALL USING (auth.role() = 'authenticated');
CREATE POLICY p6_select ON public.departments FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY p6_modify ON public.departments FOR ALL USING (EXISTS (SELECT 1 FROM public.profiles WHERE profiles.id = auth.uid() AND profiles.role = 'Admin'));

-- 5. Données de test (Départements)
INSERT INTO public.departments (code, name, description) VALUES
('MH','Mateur Haut','Site Mateur Haut'),
('SB','Sousse Bouficha','Site Sousse Bouficha'),
('MS','Menzel Salim','Site Menzel Salim'),
('MN','Menzel Nour','Site Menzel Nour'),
('LTN1','LEONI Tunisie 1','Site principal'),
('LTN2','LEONI Tunisie 2','Site secondaire'),
('LTN3','LEONI Tunisie 3','Site tertiaire')
ON CONFLICT (code) DO NOTHING;

-- 6. Données de test (Machines LEONI réelles)
INSERT INTO public.machines (tag_reference, standard_equipment_name, plant, area, serial_number, immobilisation_number, cao_number, year_of_construction, equipment_status) VALUES
('079278000000000000001005', 'Komax--Gamma 333',                         'MH',   'Cutting & Wire Preparation',    '1788',                            '2235330902', '808', 2025, 'Active'),
('07927800000000000000B52',  'Schleuniger--PowerStrip 9550',              'SB',   'Cutting & Wire Preparation',    '9550.518',                        '8234430807', '809', 2020, 'Passive'),
('07927800000000000000E0B',  'Schunk--Global-Splicer 40-Plus',            'MS',   'Cutting & Wire Preparation',    '687342-5',                        '4236550707', NULL,  2005, 'Defect'),
('07927800000000000000E62',  'Emdep--Fuse Detection and Screwing',        'MN',   'Post Assembly & Testing',       '2022TU-12379',                    '2110660904', NULL,  1998, 'Scrapped'),
('07927800000000000000E77',  'DSG Canusa--DERAY SHUTTLE 2.0 SHRINK MAC',  'LTN1', 'Special Processes',             'T04-M-44312/SB1009/2022',         '4117761002', NULL,  2015, 'TransferDone'),
('07927800000000000000D9A',  'Ductimetal--Assembly Line Standard PL6',    'LTN2', 'Assembly',                      '81-2187776',                      '8110188005', NULL,  2010, 'TransferOngoing'),
('07927800000000000000CE2',  'Vision Industrielle--Assembly Line LAD',    'LTN3', 'Assembly',                      'LAD N°01- FJX',                   '3199080404', NULL,  2000, 'TransferAvailable')
ON CONFLICT (tag_reference) DO NOTHING;
